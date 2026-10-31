using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class StorageDeletionOutboxMessageConfiguration
    : IEntityTypeConfiguration<StorageDeletionOutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<StorageDeletionOutboxMessage> builder)
    {
        builder.ToTable("storage_deletion_outbox");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.NextAttemptAtUtc)
            .IsRequired();

        builder.Property(x => x.Attempts)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(x => new
        {
            x.ProcessedAtUtc,
            x.NextAttemptAtUtc
        });
    }
}