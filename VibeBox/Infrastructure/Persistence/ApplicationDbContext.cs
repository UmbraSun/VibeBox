using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Space> Spaces => Set<Space>();

    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();

    public DbSet<Channel> Channels => Set<Channel>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    public DbSet<StorageDeletionOutboxMessage> StorageDeletionOutboxMessages => Set<StorageDeletionOutboxMessage>();

    IQueryable<User> IApplicationDbContext.Users => Users;

    public void AddStorageDeletionOutboxMessage(
        StorageDeletionOutboxMessage message)
    {
        StorageDeletionOutboxMessages.Add(message);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}