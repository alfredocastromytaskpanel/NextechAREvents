using NextechAREvents.Models;
using Microsoft.EntityFrameworkCore;

namespace NextechAREvents.Data
{
    public class EventContext : DbContext
    {
        public EventContext(DbContextOptions<EventContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<EventModel>()
                .HasMany(e => e.Attendees)
                .WithOne(a => a.Event)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<EventModel> Events { get; set; }
        public DbSet<AttendeeModel> Attendees { get; set; }
    }
}
