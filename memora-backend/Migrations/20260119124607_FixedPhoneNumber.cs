using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace memorabackend.Migrations
{
    /// <inheritdoc />
    public partial class FixedPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNubmer",
                table: "Users",
                newName: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Users",
                newName: "PhoneNubmer");
        }
    }
}
