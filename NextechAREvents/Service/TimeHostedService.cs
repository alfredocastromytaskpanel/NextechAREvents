using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextechAREvents.Controllers;
using System.Net.Http;
using Newtonsoft.Json;
using NextechAREvents.DTO;
using System.IO;

namespace NextechAREvents.Service
{
    public class TimeHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private ILogger<TimeHostedService> _logger;

        public void TimedHostedService(ILogger<TimeHostedService> logger)
        {
            this._logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using (var httpClient = new HttpClient(clientHandler))
            {

                List<EventDTO> eventList = new List<EventDTO>();
                List<EventDTO> eventListDel = new List<EventDTO>();
                try
                {
                    //Warning!! Put here the Url for this WebAPI
                    //"https://localhost:5001"
                    var response = httpClient.GetAsync("https://localhost:44331/api/SendInvite/updateevents").Result;
                    var status = response.IsSuccessStatusCode;
                    if (status)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        eventList = JsonConvert.DeserializeObject<List<EventDTO>>(content);
                    }

                    //Warning!! Put here the Url for this WebAPI
                    //"https://localhost:5001"
                    var responseDel = httpClient.DeleteAsync("https://localhost:44331/api/SendInvite").Result;
                    var statusDel = responseDel.IsSuccessStatusCode;
                    if (statusDel)
                    {
                        var content = responseDel.Content.ReadAsStringAsync().Result;
                        eventListDel = JsonConvert.DeserializeObject<List<EventDTO>>(content);
                    }

                    string msg = string.Format("Polling Update - DateTime: {0} - Status: {1}. {2} Events updated", DateTime.Now.ToString(), status ? "OK" : "Fail", eventList.Count);
                    string msgDel = string.Format("Polling Delete - DateTime: {0} - Status: {1}. {2} Events Deleted", DateTime.Now.ToString(), status ? "OK" : "Fail", eventListDel.Count);

                    if (_logger != null)
                        _logger.LogInformation(msg, msgDel);

                    try
                    {
                        string[] lines = { msg, msgDel };
                        File.AppendAllLines("PollingTaskLog.txt", lines);
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                            _logger.LogError(ex.ToString());
                    }

                    Console.WriteLine(msg);
                }
                catch (Exception e)
                {
                    if (_logger != null)
                        _logger.LogError(e.ToString());
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
