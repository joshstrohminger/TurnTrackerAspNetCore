using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TurnTrackerAspNetCore.Migrations
{
    public partial class AddInvites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invites",
                columns: table => new
                {
                    Token = table.Column<Guid>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    Email = table.Column<string>(maxLength: 256, nullable: false),
                    InviteeId = table.Column<string>(nullable: true),
                    InviterId = table.Column<string>(nullable: false),
                    Sent = table.Column<DateTimeOffset>(nullable: false),
                    Used = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invites", x => x.Token);
                    table.ForeignKey(
                        name: "FK_Invites_AspNetUsers_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invites_AspNetUsers_InviterId",
                        column: x => x.InviterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invites_InviteeId",
                table: "Invites",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invites_InviterId",
                table: "Invites",
                column: "InviterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invites");
        }
    }
}
