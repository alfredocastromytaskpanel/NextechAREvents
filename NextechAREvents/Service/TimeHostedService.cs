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

namespace NextechAREvents.Service
{
    public class TimeHostedService : IHostedService, IDisposable
    {
        private Timer _timer;

        public void TimedHostedService()
        {

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(DoWork, null, TimeSpan.Zero,
            //    TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using (var httpClient = new HttpClient(clientHandler))
            {

                List<EventDTO> eventList = new List<EventDTO>();
                try
                {
                    //"https://localhost:5001"
                    var response = httpClient.GetAsync("https://localhost:44331/api/SendInvite/updateevents").Result;

                    var status = response.IsSuccessStatusCode;
                    if (status)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        eventList = JsonConvert.DeserializeObject<List<EventDTO>>(content);
                    }

                    Console.WriteLine(string.Format("Status: {0}. {1} Events updated", status ? "OK": "Fail", eventList.Count));
                }
                catch (Exception e)
                {
                    
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
