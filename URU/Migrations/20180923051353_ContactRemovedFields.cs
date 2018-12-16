using Microsoft.EntityFrameworkCore.Migrations;

namespace URU.Migrations
{
    public partial class ContactRemovedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Matter",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "PhoneCode",
                table: "Contacts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Matter",
                table: "Contacts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneCode",
                table: "Contacts",
                nullable: true);
        }
    }
}
