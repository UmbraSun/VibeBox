using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToMessageId",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ReplyToMessageId",
                table: "messages",
                column: "ReplyToMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_messages_ReplyToMessageId",
                table: "messages",
                column: "ReplyToMessageId",
                principalTable: "messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_messages_ReplyToMessageId",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_messages_ReplyToMessageId",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "messages");
        }
    }
}
