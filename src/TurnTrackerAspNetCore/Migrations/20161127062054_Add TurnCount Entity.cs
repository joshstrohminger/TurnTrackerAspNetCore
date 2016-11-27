using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TurnTrackerAspNetCore.Migrations
{
    public partial class AddTurnCountEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turns_Tasks_TrackedTaskId",
                table: "Turns");

            migrationBuilder.RenameColumn(
                name: "TrackedTaskId",
                table: "Turns",
                newName: "TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Turns_TrackedTaskId",
                table: "Turns",
                newName: "IX_Turns_TaskId");

            migrationBuilder.CreateTable(
                name: "TurnCounts",
                columns: table => new
                {
                    TaskId = table.Column<long>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    TaskName = table.Column<string>(nullable: true),
                    TotalTurns = table.Column<int>(nullable: false),
                    UserName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnCounts", x => new { x.TaskId, x.UserId });
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Turns_Tasks_TaskId",
                table: "Turns",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turns_Tasks_TaskId",
                table: "Turns");

            migrationBuilder.DropTable(
                name: "TurnCounts");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "Turns",
                newName: "TrackedTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Turns_TaskId",
                table: "Turns",
                newName: "IX_Turns_TrackedTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turns_Tasks_TrackedTaskId",
                table: "Turns",
                column: "TrackedTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
