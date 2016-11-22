using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TurnTrackerAspNetCore.Entities
{
    public class TurnTrackerDbContext : IdentityDbContext<User>
    {
        public DbSet<TrackedTask> Tasks { get; set; }

        public DbSet<Turn> Turns { get; set; }

        public TurnTrackerDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var turns = modelBuilder.Entity<Turn>();
            turns.Property(x => x.Created).HasDefaultValue(DateTimeOffset.UtcNow);
            turns.Property(x => x.Modified).HasDefaultValue(DateTimeOffset.UtcNow);
            turns.Property(x => x.Taken).HasDefaultValue(DateTimeOffset.UtcNow);

            var tasks = modelBuilder.Entity<TrackedTask>();
            tasks.Property(x => x.Created).HasDefaultValue(DateTimeOffset.UtcNow);
            tasks.Property(x => x.Modified).HasDefaultValue(DateTimeOffset.UtcNow);
        }
    }
}
