using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TurnTrackerAspNetCore.Migrations
{
    public partial class FixDefaultDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Taken",
                table: "Turns",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Modified",
                table: "Turns",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "Turns",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Modified",
                table: "Tasks",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "Tasks",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "GETDATE()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Taken",
                table: "Turns",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Modified",
                table: "Turns",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "Turns",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Modified",
                table: "Tasks",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "Tasks",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");
        }
    }
}
