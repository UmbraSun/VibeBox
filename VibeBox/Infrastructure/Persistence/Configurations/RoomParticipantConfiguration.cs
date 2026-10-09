using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class RoomParticipantConfiguration
    : IEntityTypeConfiguration<RoomParticipant>
{
    public void Configure(
        EntityTypeBuilder<RoomParticipant> builder)
    {
        builder.ToTable("room_participants");

        builder.HasKey(x => new
        {
            x.RoomId,
            x.UserId
        });

        builder.Property(x => x.JoinedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Room)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}