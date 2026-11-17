using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codexo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReasoningLevelToUserPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReasoningLevel",
                table: "UserPreferences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "low");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasoningLevel",
                table: "UserPreferences");
        }
    }
}
