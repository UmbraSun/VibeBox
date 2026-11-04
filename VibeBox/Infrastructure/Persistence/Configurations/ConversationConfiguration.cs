using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", table =>
        {
            table.HasCheckConstraint(
                "CK_conversations_TypeAndDirectUsers",
                """
                (
                    "Type" = 0
                    AND "Name" IS NULL
                    AND "DirectUserLowId" IS NOT NULL
                    AND "DirectUserHighId" IS NOT NULL
                    AND "DirectUserLowId" < "DirectUserHighId"
                )
                OR
                (
                    "Type" = 1
                    AND "Name" IS NOT NULL
                    AND "DirectUserLowId" IS NULL
                    AND "DirectUserHighId" IS NULL
                )
                """);
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastMessageAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DirectUserLowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DirectUserHighId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.DirectUserLowId,
            x.DirectUserHighId
        })
            .IsUnique()
            .HasFilter(
                "\"DirectUserLowId\" IS NOT NULL AND \"DirectUserHighId\" IS NOT NULL");

        builder.HasIndex(x => x.LastMessageAtUtc);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}