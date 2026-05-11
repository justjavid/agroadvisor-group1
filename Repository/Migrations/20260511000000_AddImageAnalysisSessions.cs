using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddImageAnalysisSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make existing string columns nullable to match new model.
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl", table: "AnalysisData",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PlantName", table: "AnalysisData",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DiseaseName", table: "AnalysisData",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            // New columns for session tracking.
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId", table: "AnalysisData",
                type: "uniqueidentifier", nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<string>(
                name: "UserId", table: "AnalysisData",
                type: "nvarchar(128)", maxLength: 128, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prompt", table: "AnalysisData",
                type: "nvarchar(max)", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiResponseJson", table: "AnalysisData",
                type: "nvarchar(max)", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisData_UserId_CreatedDate",
                table: "AnalysisData",
                columns: new[] { "UserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisData_SessionId",
                table: "AnalysisData",
                column: "SessionId",
                unique: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AnalysisData_SessionId", table: "AnalysisData");
            migrationBuilder.DropIndex(name: "IX_AnalysisData_UserId_CreatedDate", table: "AnalysisData");

            migrationBuilder.DropColumn(name: "AiResponseJson", table: "AnalysisData");
            migrationBuilder.DropColumn(name: "Prompt", table: "AnalysisData");
            migrationBuilder.DropColumn(name: "UserId", table: "AnalysisData");
            migrationBuilder.DropColumn(name: "SessionId", table: "AnalysisData");

            migrationBuilder.AlterColumn<string>(
                name: "DiseaseName", table: "AnalysisData",
                type: "nvarchar(max)", nullable: false, defaultValue: "",
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlantName", table: "AnalysisData",
                type: "nvarchar(max)", nullable: false, defaultValue: "",
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl", table: "AnalysisData",
                type: "nvarchar(max)", nullable: false, defaultValue: "",
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
        }
    }
}
