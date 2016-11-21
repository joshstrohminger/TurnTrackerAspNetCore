using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Migrations
{
    [DbContext(typeof(TurnTrackerDbContext))]
    [Migration("20161121081240_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("TurnTrackerAspNetCore.Entities.TrackedTask", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedUtc");

                    b.Property<DateTime>("ModifiedUtc");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<decimal>("Period");

                    b.Property<bool>("TeamBased");

                    b.Property<int>("Unit");

                    b.HasKey("Id");

                    b.ToTable("Tasks");
                });

            modelBuilder.Entity("TurnTrackerAspNetCore.Entities.Turn", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedUtc");

                    b.Property<DateTime>("ModifiedUtc");

                    b.Property<DateTime>("TakenUtc");

                    b.Property<long>("TrackedTaskId");

                    b.HasKey("Id");

                    b.HasIndex("TrackedTaskId");

                    b.ToTable("Turns");
                });

            modelBuilder.Entity("TurnTrackerAspNetCore.Entities.Turn", b =>
                {
                    b.HasOne("TurnTrackerAspNetCore.Entities.TrackedTask", "Task")
                        .WithMany("Turns")
                        .HasForeignKey("TrackedTaskId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
