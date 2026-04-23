using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class NAME : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "chat_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "chat_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "chat_sessions",
                type: "timestamp with time zone",
                nullable: true);

            // 🔥 FIX: string -> enum(int)
            migrationBuilder.Sql(@"
                ALTER TABLE chat_messages 
                ALTER COLUMN ""Role"" TYPE integer 
                USING CASE 
                    WHEN ""Role"" = 'user' THEN 0
                    WHEN ""Role"" = 'assistant' THEN 1
                    WHEN ""Role"" = 'system' THEN 2
                    ELSE 0
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "chat_sessions");

            // 🔙 reverse conversion
            migrationBuilder.Sql(@"
                ALTER TABLE chat_messages 
                ALTER COLUMN ""Role"" TYPE text 
                USING CASE 
                    WHEN ""Role"" = 0 THEN 'user'
                    WHEN ""Role"" = 1 THEN 'assistant'
                    WHEN ""Role"" = 2 THEN 'system'
                    ELSE 'user'
                END;
            ");
        }
    }
}