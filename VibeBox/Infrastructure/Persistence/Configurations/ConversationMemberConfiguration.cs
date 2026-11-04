using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ConversationMemberConfiguration
    : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("conversation_members");

        builder.HasKey(x => new
        {
            x.ConversationId,
            x.UserId
        });

        builder.Property(x => x.JoinedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastReadAtUtc);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
    }
}