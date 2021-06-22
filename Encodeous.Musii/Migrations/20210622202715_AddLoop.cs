using Microsoft.EntityFrameworkCore.Migrations;

namespace Encodeous.Musii.Migrations
{
    public partial class AddLoop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsLooped",
                table: "Records",
                newName: "Loop");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Loop",
                table: "Records",
                newName: "IsLooped");
        }
    }
}
