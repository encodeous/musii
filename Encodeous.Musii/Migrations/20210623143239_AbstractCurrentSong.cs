using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Encodeous.Musii.Migrations
{
    public partial class AbstractCurrentSong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "CurrentPosition",
                table: "Records",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPosition",
                table: "Records");
        }
    }
}
