using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextechAREvents.Extensions;

namespace NextechAREvents.DTO
{
    public class InfernoEventDTO
    {
        public string id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string clientId { get; set; }

        public DateTimeOffset preRoll { get; set; }

        public DateTimeOffset startTime { get; set; }

        public Microsoft.Graph.Event ToMSGraphEvent()
        {
            var tzName = startTime.GetTimeZoneStandardName();

            var newEvent = new Event
            {
                Subject = name, 
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = name + Environment.NewLine + description 
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = startTime.DateTime.ToString("yyyy-MM-ddTHH:mm:ss"), 
                    TimeZone = tzName 
                },
                End = new DateTimeTimeZone
                {
                    DateTime = startTime.DateTime.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"), 
                    TimeZone = tzName 
                },
                ResponseRequested = false,
                AllowNewTimeProposals = false
            };
            return newEvent;
        }
    }
}
