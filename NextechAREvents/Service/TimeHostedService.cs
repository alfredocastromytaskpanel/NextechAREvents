using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextechAREvents.Controllers;
using System.Net.Http;

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
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using (var httpClient = new HttpClient(clientHandler))
            {
                try
                {
                    var response = httpClient.GetAsync("https://localhost:5001/api/SendInvite/updateevents").Result;

                    var status = response.IsSuccessStatusCode;
                    Console.WriteLine(status);
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
