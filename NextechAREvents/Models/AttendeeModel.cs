using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.Models
{
    public class AttendeeModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Mail { get; set; }

        public int? InfernoUserId { get; set; }

        [Required]
        public int EventId { get; set; }

        public EventModel Event { get; set; }
    }
}
