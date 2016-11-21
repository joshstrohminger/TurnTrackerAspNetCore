﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TurnTrackerAspNetCore.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2016, 11, 21, 9, 30, 1, 283, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    Modified = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2016, 11, 21, 9, 30, 1, 283, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Period = table.Column<decimal>(nullable: false),
                    TeamBased = table.Column<bool>(nullable: false),
                    Unit = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2016, 11, 21, 9, 30, 1, 271, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    Modified = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2016, 11, 21, 9, 30, 1, 283, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    Taken = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2016, 11, 21, 9, 30, 1, 283, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    TrackedTaskId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turns_Tasks_TrackedTaskId",
                        column: x => x.TrackedTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Turns_TrackedTaskId",
                table: "Turns",
                column: "TrackedTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Turns");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}