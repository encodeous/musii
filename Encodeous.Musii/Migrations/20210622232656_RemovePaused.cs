using Microsoft.EntityFrameworkCore.Migrations;

namespace Encodeous.Musii.Migrations
{
    public partial class RemovePaused : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "Records");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "Records",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
