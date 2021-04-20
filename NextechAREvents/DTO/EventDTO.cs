using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.DTO
{
    public class EventDTO
    {
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public string Start { get; set; }
        [Required]
        public string End { get; set; }
        [Required]
        public string TimeZone { get; set; }
        [Required]
        public string TenantId { get; set; }
    }
}
