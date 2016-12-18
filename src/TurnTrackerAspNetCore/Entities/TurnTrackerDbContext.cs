using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TurnTrackerAspNetCore.Entities
{
    public class TurnTrackerDbContext : IdentityDbContext<User>
    {
        public DbSet<TrackedTask> Tasks { get; set; }

        public DbSet<Turn> Turns { get; set; }

        public DbSet<Participant> Participants { get; set; }

        public DbSet<TurnCount> TurnCounts { get; set; }

        public DbSet<SiteSetting> SiteSettings { get; set; }

        public DbSet<Invite> Invites { get; set; }

        public TurnTrackerDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var turns = modelBuilder.Entity<Turn>();
            turns.Property(x => x.Created).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");
            turns.Property(x => x.Modified).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");
            turns.Property(x => x.Taken).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");

            var tasks = modelBuilder.Entity<TrackedTask>();
            tasks.Property(x => x.Created).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");
            tasks.Property(x => x.Modified).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");

            var invites = modelBuilder.Entity<Invite>();
            invites.Property(x => x.Created).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIMEOFFSET()");

            var participants = modelBuilder.Entity<Participant>();
            participants.HasKey(x => new {x.TaskId, x.UserId});
            participants.HasOne(x => x.Task).WithMany(x => x.Participants).HasForeignKey(x => x.TaskId);
            participants.HasOne(x => x.User).WithMany(x => x.Participations).HasForeignKey(x => x.UserId);

            var turnCounts = modelBuilder.Entity<TurnCount>();
            turnCounts.HasKey(x => new { x.TaskId, x.UserId });
        }
    }
}
