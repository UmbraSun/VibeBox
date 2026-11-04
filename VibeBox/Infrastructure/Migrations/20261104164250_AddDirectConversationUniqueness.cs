using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectConversationUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectUserHighId",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DirectUserLowId",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_DirectUserHighId",
                table: "conversations",
                column: "DirectUserHighId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_DirectUserLowId_DirectUserHighId",
                table: "conversations",
                columns: new[] { "DirectUserLowId", "DirectUserHighId" },
                unique: true,
                filter: "\"DirectUserLowId\" IS NOT NULL AND \"DirectUserHighId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_conversations_TypeAndDirectUsers",
                table: "conversations",
                sql: "(\r\n    \"Type\" = 0\r\n    AND \"Name\" IS NULL\r\n    AND \"DirectUserLowId\" IS NOT NULL\r\n    AND \"DirectUserHighId\" IS NOT NULL\r\n    AND \"DirectUserLowId\" < \"DirectUserHighId\"\r\n)\r\nOR\r\n(\r\n    \"Type\" = 1\r\n    AND \"Name\" IS NOT NULL\r\n    AND \"DirectUserLowId\" IS NULL\r\n    AND \"DirectUserHighId\" IS NULL\r\n)");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_users_DirectUserHighId",
                table: "conversations",
                column: "DirectUserHighId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_users_DirectUserLowId",
                table: "conversations",
                column: "DirectUserLowId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_users_DirectUserHighId",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_conversations_users_DirectUserLowId",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_DirectUserHighId",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_DirectUserLowId_DirectUserHighId",
                table: "conversations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_conversations_TypeAndDirectUsers",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "DirectUserHighId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "DirectUserLowId",
                table: "conversations");
        }
    }
}
