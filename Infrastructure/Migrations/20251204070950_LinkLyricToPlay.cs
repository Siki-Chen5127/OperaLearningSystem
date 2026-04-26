using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperaLearningSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkLyricToPlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "OperaLyrics");

            migrationBuilder.AddColumn<int>(
                name: "PlayId",
                table: "OperaLyrics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceText",
                table: "OperaLyrics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperaLyrics_PlayId",
                table: "OperaLyrics",
                column: "PlayId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperaLyrics_Plays_PlayId",
                table: "OperaLyrics",
                column: "PlayId",
                principalTable: "Plays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperaLyrics_Plays_PlayId",
                table: "OperaLyrics");

            migrationBuilder.DropIndex(
                name: "IX_OperaLyrics_PlayId",
                table: "OperaLyrics");

            migrationBuilder.DropColumn(
                name: "PlayId",
                table: "OperaLyrics");

            migrationBuilder.DropColumn(
                name: "SourceText",
                table: "OperaLyrics");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "OperaLyrics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
