using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TurnTrackerAspNetCore.Migrations
{
    public partial class OptionalInviteProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Used",
                table: "Invites",
                nullable: true,
                oldClrType: typeof(DateTimeOffset));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Sent",
                table: "Invites",
                nullable: true,
                oldClrType: typeof(DateTimeOffset));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Expires",
                table: "Invites",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Expires",
                table: "Invites");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Used",
                table: "Invites",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Sent",
                table: "Invites",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);
        }
    }
}
