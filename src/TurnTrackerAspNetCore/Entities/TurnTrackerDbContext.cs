using Microsoft.EntityFrameworkCore;

namespace TurnTrackerAspNetCore.Entities
{
    public class TurnTrackerDbContext : DbContext
    {
        public DbSet<TrackedTask> Tasks { get; set; }
        public DbSet<Turn> Turns { get; set; }

        public TurnTrackerDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
