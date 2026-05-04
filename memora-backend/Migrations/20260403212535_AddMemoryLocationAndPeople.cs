using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace memorabackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryLocationAndPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Memory",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Memory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Memory",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemoryPerson",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryPerson", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryPerson_Memory_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryPerson_MemoryId",
                table: "MemoryPerson",
                column: "MemoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryPerson");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Memory");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Memory");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Memory");
        }
    }
}
