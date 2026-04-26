using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperaLearningSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUgcAuditSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                table: "Plays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmitterId",
                table: "Plays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                table: "Masters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmitterId",
                table: "Masters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmitterId",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmitterId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plays_SubmitterId",
                table: "Plays",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Masters_SubmitterId",
                table: "Masters",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubmitterId",
                table: "Courses",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SubmitterId",
                table: "Categories",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminApplications_UserId",
                table: "AdminApplications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_SubmitterId",
                table: "Categories",
                column: "SubmitterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_SubmitterId",
                table: "Courses",
                column: "SubmitterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Masters_AspNetUsers_SubmitterId",
                table: "Masters",
                column: "SubmitterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Plays_AspNetUsers_SubmitterId",
                table: "Plays",
                column: "SubmitterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_SubmitterId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_SubmitterId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Masters_AspNetUsers_SubmitterId",
                table: "Masters");

            migrationBuilder.DropForeignKey(
                name: "FK_Plays_AspNetUsers_SubmitterId",
                table: "Plays");

            migrationBuilder.DropTable(
                name: "AdminApplications");

            migrationBuilder.DropIndex(
                name: "IX_Plays_SubmitterId",
                table: "Plays");

            migrationBuilder.DropIndex(
                name: "IX_Masters_SubmitterId",
                table: "Masters");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SubmitterId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Categories_SubmitterId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "Plays");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Plays");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "Masters");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Masters");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Categories");
        }
    }
}
