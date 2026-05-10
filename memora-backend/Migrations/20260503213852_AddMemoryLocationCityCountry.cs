using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace memorabackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryLocationCityCountry : Migration
    {
        /// <inheritdoc />
       protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationCity",
                table: "Memory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationCountry",
                table: "Memory",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationCity",
                table: "Memory");

            migrationBuilder.DropColumn(
                name: "LocationCountry",
                table: "Memory");
        }
    }
}
