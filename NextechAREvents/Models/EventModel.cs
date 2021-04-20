using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.Models
{
    public class EventModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Inferno Event Id")]
        public string InfernoEventId { get; set; }

        [Required]
        [Display(Name = "MS Graph Event Id")]
        public string MSGraphEventId { get; set; }

        [Required]
        public string OrganizerEmail { get; set; }

        [Required]
        public string OrganizerName { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        [Required]
        public string StartTZ { get; set; }

        [Required]
        public string EndTZ { get; set; }

        [Required]
        public string TimeZone { get; set; }

        [Required]
        public DateTime Start { get; set; }

        [Required]
        public DateTime End { get; set; }

        [Required]
        public DateTimeOffset OriginalStart { get; set; }

        [Required]
        public string OriginalStartTimeZone { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public ICollection<AttendeeModel> Attendees { get; set; }
    }
}
