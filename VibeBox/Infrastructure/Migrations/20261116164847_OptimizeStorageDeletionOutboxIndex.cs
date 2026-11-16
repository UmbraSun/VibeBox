using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeStorageDeletionOutboxIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_storage_deletion_outbox_ProcessedAtUtc_NextAttemptAtUtc",
                table: "storage_deletion_outbox");

            migrationBuilder.CreateIndex(
                name: "IX_storage_deletion_outbox_ProcessedAtUtc_NextAttemptAtUtc_Loc~",
                table: "storage_deletion_outbox",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "LockedUntilUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_storage_deletion_outbox_ProcessedAtUtc_NextAttemptAtUtc_Loc~",
                table: "storage_deletion_outbox");

            migrationBuilder.CreateIndex(
                name: "IX_storage_deletion_outbox_ProcessedAtUtc_NextAttemptAtUtc",
                table: "storage_deletion_outbox",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc" });
        }
    }
}
