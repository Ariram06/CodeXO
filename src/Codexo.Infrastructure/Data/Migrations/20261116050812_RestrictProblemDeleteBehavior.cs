using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codexo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictProblemDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProblemSessions_Problems_ProblemId",
                table: "ProblemSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ProblemSessions_Problems_ProblemId",
                table: "ProblemSessions",
                column: "ProblemId",
                principalTable: "Problems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProblemSessions_Problems_ProblemId",
                table: "ProblemSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ProblemSessions_Problems_ProblemId",
                table: "ProblemSessions",
                column: "ProblemId",
                principalTable: "Problems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
