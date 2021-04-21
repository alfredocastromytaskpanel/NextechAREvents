// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SendInviteController.cs" company="Jolokia">
//   Copyright Jolokia, All rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Graph.Extensions;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using NextechAREvents.Data;
using NextechAREvents.DTO;
using NextechAREvents.Extensions;
using NextechAREvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NextechAREvents.Controllers
{
    public class BaseController : ControllerBase
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IWebHostEnvironment _hostEnv;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<BaseController> _logger;

        private readonly string _graphEndpoint;
        private readonly string _defaultOrganizerUserId;
        private readonly string _infernoAPIUrl;
        private readonly string _infernoAPIKey;
        private readonly string _clientId;
        private readonly string _tenantID;
        private readonly string _clientSecret;

        public BaseController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment hostEnv,
            IConfiguration configuration,
            ILogger<SendInviteController> logger)
        {
            this._unitOfWork = unitOfWork;
            this._hostEnv = hostEnv;
            this._configuration = configuration;
            this._logger = logger;

            _infernoAPIUrl = _configuration.GetValue<string>("InfernoAPIUrl");
            _infernoAPIKey = _configuration.GetValue<string>("InfernoAPIKey");

            _defaultOrganizerUserId = _configuration.GetValue<string>("AzureAd:DefaultOrganizerUserId");
            _clientId = _configuration.GetValue<string>("AzureAd:ClientId");
            _tenantID = _configuration.GetValue<string>("AzureAd:TenantId");
            _clientSecret = _configuration.GetValue<string>("AzureAd:ClientSecret");
        }

        /// <summary>
        /// Create a MS Graph Event from Inferno Event in the current user's and recipient's calendar and send a notification email.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="recipients"></param>
        /// <returns>The created MS Graph event</returns>
        protected async Task<Event> CreateEventAndSendEmail(string eventId, string recipients)
        {
            Event newEvent = null;
            try
            {
                GraphServiceClient graphClient = GetGraphServiceClient(_tenantID, _clientId, _clientSecret);

                //Get Event From Inferno API and create, then create MSGraph Event and send Email invitation using MSGraph API
                newEvent = await CreateEvent(graphClient, _hostEnv, _infernoAPIUrl, _infernoAPIKey, _defaultOrganizerUserId, eventId, recipients);

                if (newEvent != null)
                {
                    //Save Event in DB
                    var dbEvent = newEvent.ToEventModel(eventId);
                    await _unitOfWork.EventRepository.InsertAsync(dbEvent);

                    //Create and send Email invitation using MSGraph API
                    await SendEmail(graphClient, _hostEnv, newEvent, _defaultOrganizerUserId, recipients, dbEvent.OrganizerEmail);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return newEvent;
        }

        /// <summary>
        /// Check for all events sent and stored in DB and update if the the Start Time is change,  
        /// Update the event in calendar and send a notification email.
        /// </summary>
        /// <returns>The created MS Graph event</returns>
        protected async Task<Event> UpdateEventAndSendEmail(EventModel curEvent, InfernoEventDTO infernoEvent)
        {
            var updatedEvent = infernoEvent.ToMSGraphEvent();

            Event eventUpdated = null;

            try
            {
                GraphServiceClient graphClient = GetGraphServiceClient(_tenantID, _clientId, _clientSecret);

                string eventId = curEvent.MSGraphEventId;

                //Only changes in Start and/or End Times
                Event eventToUpdate = new Event
                {
                    Start = updatedEvent.Start,
                    End = updatedEvent.End
                };

                eventUpdated = await graphClient.Users[_defaultOrganizerUserId]
                                                .Events[eventId]
                                                .Request()
                                                .UpdateAsync(eventToUpdate);
                if (eventUpdated != null)
                {
                    //Save Event in DB
                    curEvent.StartTZ = eventUpdated.Start.DateTime;
                    curEvent.EndTZ = eventUpdated.End.DateTime;
                    curEvent.TimeZone = eventUpdated.Start.TimeZone;
                    curEvent.Start = eventUpdated.Start.ToDateTime();
                    curEvent.End = eventUpdated.End.ToDateTime();
                    curEvent.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.EventRepository.UpdateAsync(curEvent);

                    string recipients = string.Join(";", eventUpdated.Attendees.Select(x => x.EmailAddress.Address).ToList());

                    //Create and send Email invitation using MSGraph API
                    await SendEmail(graphClient, _hostEnv, eventUpdated, _defaultOrganizerUserId, recipients, curEvent.OrganizerEmail);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return eventUpdated;
        }

        /// <summary>
        /// Check for all events sent and stored in DB and delete those events in calendars if they are expired 1 hour ago,  
        /// </summary>
        /// <param name="curEvent"></param>
        /// <returns></returns>
        protected async Task<bool> DeleteEvent(EventModel curEvent)
        {
            try
            {
                GraphServiceClient graphClient = GetGraphServiceClient(_tenantID, _clientId, _clientSecret);

                string eventId = curEvent.MSGraphEventId;

                await graphClient.Users[_defaultOrganizerUserId]
                                 .Events[eventId]
                                 .Request()
                                 .DeleteAsync();

                await _unitOfWork.EventRepository.DeleteAsync(curEvent);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Create a MS Graph Event from Inferno Event in the current user's and recipient's calendar
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="hostEnv"></param>
        /// <param name="infernoAPIKey"></param>
        /// <param name="eventId"></param>
        /// <param name="recipients"></param>
        /// <returns>The created MS Graph event</returns>
        private static async Task<Event> CreateEvent(
            GraphServiceClient graphClient, IWebHostEnvironment hostEnv,
            string infernoAPIUrl, string infernoAPIKey, string defaultOrganizerUserId, 
            string eventId, string recipients)
        {
            //TODO Check if the Event already exists in the DB for the recipients

            Event createdEvent = null;

            User me = await graphClient.Users[defaultOrganizerUserId].Request().GetAsync();

            // Get InfernoEvent info from Inferno WebAPI
            InfernoEventDTO infernoEvent = await GetInfernoEvent(infernoAPIUrl, infernoAPIKey, eventId);
            if (infernoEvent == null)
                return null;

            //Construct a MSGraph Event from InfernoEvent
            string tzName = infernoEvent.startTime.GetTimeZoneStandardName();
            Event newEvent = infernoEvent.ToMSGraphEvent();

            //Add default user and recipients as attendees to the MSGraph event
            newEvent = AddAttendees(newEvent, recipients, me);
            tzName = "UTC";

            try
            {
                createdEvent = await graphClient.Users[defaultOrganizerUserId]
                                            .Events
                                            .Request()
                                            .Header("Prefer", $"outlook.timezone=\"{tzName}\"")
                                            .AddAsync(newEvent);
            }catch(Exception e)
            {

            }
            

            return createdEvent;
        }

        /// <summary>
        /// Send event notification email from current user to recipients 
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="hostEnv"></param>
        /// <param name="newEvent"></param>
        /// <param name="defaultOrganizerUserId"></param>
        /// <param name="recipients"></param>
        /// <param name="fromEmail"></param>
        private static async Task SendEmail(
             GraphServiceClient graphClient, IWebHostEnvironment hostEnv, Event newEvent, 
             string defaultOrganizerUserId, string recipients, string fromEmail)
        {
            //Construct a MSGraph Email notification from current logged user and to all event attendees
            var email = CreateMSGraphEmail(hostEnv, recipients, fromEmail, newEvent);

            //Send email invitation to all event attendees using MSGraph
            await graphClient.Users[defaultOrganizerUserId].SendMail(email, true).Request().PostAsync();
        }

        /// <summary>
        /// Get the Event from Inferno WebAPI
        /// </summary>
        /// <param name="infernoAPIKey"></param>
        /// <param name="eventId"></param>
        /// <returns>InfernoEventDTO object</returns>
        protected static async Task<InfernoEventDTO> GetInfernoEvent(string infernoAPIUrl, string infernoAPIKey, string eventId)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + infernoAPIKey);
                var response = await httpClient.GetAsync(infernoAPIUrl + eventId);
                if (!response.IsSuccessStatusCode) return null;
                string apiResponse = await response.Content.ReadAsStringAsync();
                var infEvent = JsonConvert.DeserializeObject<InfernoEventDTO>(apiResponse);
                return infEvent;
            }
        }

        /// <summary>
        /// Create a default MS Graph Event, just for debugging
        /// </summary>
        /// <param name="tzName"></param>
        /// <returns>The MS Graph event</returns>
        private static Event GetDefaultEventOnError(string tzName)
        {
            Event newEvent = new Event
            {
                Subject = "New demo event",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = "This is the content for a new demo event."
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = tzName
                },
                End = new DateTimeTimeZone
                {
                    DateTime = DateTime.Now.AddDays(1).AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = tzName
                },
                ResponseRequested = false,
                AllowNewTimeProposals = false
            };
            return newEvent;
        }

        /// <summary>
        /// Add current user and all recipients as attendees to the MS Graph Event
        /// </summary>
        /// <param name="newEvent"></param>
        /// <param name="recipients"></param>
        /// <param name="me"></param>
        /// <returns>The MS Graph event</returns>
        private static Event AddAttendees(Event newEvent, string recipients, User me)
        {
            var attendees = new List<Attendee>();
            attendees.Add(
                new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = me.Mail,
                        Name = me.DisplayName
                    },
                    Type = AttendeeType.Required //TODO From appsettings
                }
            );

            //Add recipient list to this Event
            var recipList = recipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var item in recipList)
            {
                attendees.Add(
                        new Attendee
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = item,
                                Name = item
                            },
                            Type = AttendeeType.Required //TODO From appsettings
                        }
                    );
            }

            newEvent.Attendees = attendees;

            return newEvent;
        }

        /// <summary>
        /// Create a MS Graph Email to notify all attendees
        /// </summary>
        /// <param name="hostEnv"></param>
        /// <param name="recipients"></param>
        /// <param name="meEmail"></param>
        /// <param name="newEvent"></param>
        /// <returns>The MS Graph Email</returns>
        private static Message CreateMSGraphEmail(
            IWebHostEnvironment hostEnv, string recipients, string meEmail, Event newEvent)
        {
            string path = hostEnv.WebRootPath ?? hostEnv.ContentRootPath;
            string content = System.IO.File.ReadAllText(path + "/email_template.html"); //TODO Get email_template filename from appsettings

            content = content.Replace("{Subject}", newEvent.Subject)
                .Replace("{Start}", newEvent.Start.DateTime)
                .Replace("{End}", newEvent.End.DateTime)
                .Replace("{TimeZone}", newEvent.Start.TimeZone);

            var recipList = recipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var recipientList = recipList.Select(recipient => new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = recipient.Trim()
                }
            }).ToList();

            recipientList.Add(
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = meEmail
                    }
                }
            );

            // Build the email message.
            var email = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Html,
                },
                Subject = $"Invitation to {newEvent.Subject}",
                ToRecipients = recipientList
            };

            return email;
        }

        /// <summary>
        /// Get paginated user list from the current user AzureAD
        /// </summary>
        /// <returns>String list with users emails</returns>
        protected async Task<List<string>> GetUserList()
        {
            try
            {
                List<string> userList = new List<string>();

                IEnumerable<User> users = await GetUserList(_tenantID, _clientId, _clientSecret);

                if (users != null)
                {
                    userList = users.Select(x => x.Mail).ToList();
                }

                return userList;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get paginated user list from the current user AzureAD
        /// </summary>
        /// <param name="_tenantID"></param>
        /// <param name="_clientId"></param>
        /// <param name="_clientSecret"></param>
        /// <returns>MS Graph user enumerable with users emails</returns>
        private static async Task<IEnumerable<User>> GetUserList(string _tenantID, string _clientId, string _clientSecret)
        {
            GraphServiceClient graphServiceClient = GetGraphServiceClient(_tenantID, _clientId, _clientSecret);

            IGraphServiceUsersCollectionPage users = await graphServiceClient.Users.Request()
                                                      .Filter($"accountEnabled eq true")
                                                      .Select("mail, displayName")
                                                      .GetAsync();
            return users;
        }

        private static GraphServiceClient GetGraphServiceClient(string tenantID, string clientId, string clientSecret)
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                                                                            .Create(clientId)
                                                                            .WithTenantId(tenantID)
                                                                            .WithClientSecret(clientSecret)
                                                                            .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

            GraphServiceClient graphServiceClient = new GraphServiceClient(authProvider);

            return graphServiceClient;
        }

    }
}
