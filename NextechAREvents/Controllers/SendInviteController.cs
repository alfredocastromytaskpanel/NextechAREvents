// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SendInviteController.cs" company="Jolokia">
//   Copyright Jolokia, All rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Auth;
using NextechAREvents.Extensions;
using NextechAREvents.Models;
using NextechAREvents.DTO;
using NextechAREvents.Data;

namespace NextechAREvents.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SendInviteController : BaseController
    {
        public SendInviteController(
            IUnitOfWork unitOfWork, 
            IWebHostEnvironment hostEnv, 
            IConfiguration configuration, 
            ILogger<SendInviteController> logger)
            :base(unitOfWork, hostEnv, configuration, logger)
        {
        }

        /// <summary>
        /// Get all emails from users in the current user AzureAD using MS Graph.
        /// </summary>
        /// <returns>String list with all users emails.</returns>
        [HttpGet("getallusers")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<string>>> GetAllUsers()
        {
            List<string> Users = null;

            try
            {
                Users = await GetUserList();

                if (Users == null)
                {
                    _logger.LogError($"Users not found");
                    return NotFound();
                }
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return Ok(Users);
        }

        /// <summary>
        /// Get the info about the NextechAR event using the Inferno WebApi.
        /// Then create an event in the current user and recipient's calendars and send a notification email with an invitation for this event.
        /// </summary>
        /// <param name="NexTechAREventId">The Inferno Event Id in string guid format.</param>
        /// <param name="ToEmail">Semicolon separated recipients email list</param>
        /// <returns>Info from the event created using MS Graph.</returns>
        /// <response code="200">Returned when the request is authorized and the data is retrieved.</response>
        /// <response code="404">Returned when no event is found for the given <paramref name="NexTechAREventId"/> or when an error occurred while creating the MS Graph Event.</response>
        [HttpGet("{NexTechAREventId}/{ToEmail}")]
        [ProducesResponseType(typeof(EventDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EventDTO>> Get(string NexTechAREventId, string ToEmail)
        {
            //eventId = eventId ?? "248d8ea0-b518-493d-b9c1-0a9f3e4e94c7";
            //recipients = string.IsNullOrEmpty(recipients) ? "alcastronava@hotmail.com" : recipients;

            EventDTO eventDto = null;
            try
            {
                //Get Event From Inferno API and create, then create MSGraph Event and send Email invitation using MSGraph API
                Event newEvent = await CreateEventAndSendEmail(NexTechAREventId, ToEmail);
                if (newEvent == null)
                {
                    _logger.LogError($"Event was not found or an error occurred while creating the MS Graph Event.");
                    return NotFound();
                }

                eventDto = new EventDTO
                {
                    Subject = newEvent.Subject,
                    Body = newEvent.Body.Content,
                    Start = newEvent.Start.DateTime,
                    End = newEvent.End.DateTime,
                    TimeZone = newEvent.Start.TimeZone
                };
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return Ok(eventDto);
        }

        /// <summary>
        /// Lists sent events and checks for Start time changes in the InfernoAPI, 
        /// then update the events in the calendar and send a notification email with the new date and time
        /// </summary>
        /// <returns>List with updated events info</returns>
        [HttpGet("updateevents")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<EventDTO>>> UpdateEvents()
        {
            string infernoAPIUrl = _configuration.GetValue<string>("InfernoAPIUrl");
            string infernoAPIKey = _configuration.GetValue<string>("InfernoAPIKey");

            List<EventDTO> eventList = new List<EventDTO>();

            try
            {
                var allEventList = await _unitOfWork.EventRepository.FindAllAsync();

                foreach (var curEvent in allEventList)
                {
                    // Get InfernoEvent info from Inferno WebAPI
                    InfernoEventDTO infernoEvent = await GetInfernoEvent(infernoAPIUrl, infernoAPIKey, curEvent.InfernoEventId);

                    //If start or end times change
                    if (!curEvent.Start.Equals(infernoEvent.startTime.Date))
                    {
                        Event updatedEvent = await UpdateEventAndSendEmail(curEvent, infernoEvent);
                        if (updatedEvent != null)
                        {
                            var eventDto = new EventDTO
                            {
                                Subject = updatedEvent.Subject,
                                Body = updatedEvent.Body.Content,
                                Start = updatedEvent.Start.DateTime,
                                End = updatedEvent.End.DateTime,
                                TimeZone = updatedEvent.Start.TimeZone
                            };
                            eventList.Add(eventDto);
                        }
                    }
                }
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return Ok(eventList);
        }

        /// <summary>
        /// Delete sent events if they have expired
        /// </summary>
        /// <returns>List with deleted events info</returns>
        [HttpDelete()]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<EventDTO>>> DeleteEvents()
        {
            List<EventDTO> eventList = new List<EventDTO>();

            try
            {
                var allEventList = await _unitOfWork.EventRepository.FindAllAsync();

                foreach (var curEvent in allEventList)
                {
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(curEvent.TimeZone);
                    DateTime endUTC = TimeZoneInfo.ConvertTimeToUtc(curEvent.End, timeZone);
                    DateTime curUTC = DateTime.UtcNow;

                    //If the event is expired 1 hour ago
                    if (endUTC.AddHours(1) < curUTC)
                    {
                        bool isDeleted = await DeleteEvent(curEvent);

                        if (isDeleted)
                        {
                            var eventDto = new EventDTO
                            {
                                Subject = curEvent.Subject,
                                Body = curEvent.Body,
                                Start = curEvent.Start.ToString(),
                                End = curEvent.End.ToString(),
                                TimeZone = curEvent.TimeZone
                            };
                            eventList.Add(eventDto);
                        }
                    }
                }
            }
            catch (MsalUiRequiredException ex)
            {
                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await HttpContext.Response.WriteAsync("An authentication error occurred while acquiring a token for downstream API\n" + ex.ErrorCode + "\n" + ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return Ok(eventList);
        }

    }
}

