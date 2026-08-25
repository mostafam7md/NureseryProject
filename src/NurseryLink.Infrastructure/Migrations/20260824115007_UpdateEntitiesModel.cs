using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurseryLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSeeded",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSeeded",
                table: "Accounts");
        }
    }
}
