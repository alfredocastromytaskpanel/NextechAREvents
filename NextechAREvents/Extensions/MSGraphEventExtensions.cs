using NextechAREvents.Models;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.Extensions
{
    public static class MSGraphEventExtensions
    {
        public static EventModel ToEventModel(this Event newEvent, string eventId)
        {
            EventModel dbEvent = new EventModel
            {
                InfernoEventId = eventId,
                MSGraphEventId = newEvent.Id,
                OrganizerEmail = newEvent.Organizer.EmailAddress.Address,
                OrganizerName = newEvent.Organizer.EmailAddress.Name,
                Subject = newEvent.Subject,
                Body = newEvent.Body.Content,
                StartTZ = newEvent.Start.DateTime,
                EndTZ = newEvent.End.DateTime,
                TimeZone = newEvent.Start.TimeZone,
                Start = newEvent.Start.ToDateTime(),
                End = newEvent.End.ToDateTime(),
                OriginalStart = newEvent.Start.ToDateTime(),
                OriginalStartTimeZone = newEvent.Start.TimeZone,
                CreatedDate = DateTime.UtcNow
            };
            dbEvent.Attendees = new List<AttendeeModel>();
            foreach (var item in newEvent.Attendees)
            {
                AttendeeModel attendee = new AttendeeModel
                {
                    DisplayName = item.EmailAddress.Name,
                    Mail = item.EmailAddress.Address
                };
                dbEvent.Attendees.Add(attendee);
            }

            return dbEvent;
        }
    }
}
