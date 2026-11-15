
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class MessageReactionRepository
    : IMessageReactionRepository
{
    private readonly ApplicationDbContext _context;

    public MessageReactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MessageReaction?> GetByIdAsync(
        Guid messageId,
        Guid userId,
        string emoji,
        CancellationToken cancellationToken)
    {
        return await _context.MessageReactions
            .FirstOrDefaultAsync(
                reaction =>
                    reaction.MessageId == messageId &&
                    reaction.UserId == userId &&
                    reaction.Emoji == emoji,
                cancellationToken);
    }

    public async Task AddAsync(
        MessageReaction reaction,
        CancellationToken cancellationToken)
    {
        await _context.MessageReactions.AddAsync(
            reaction,
            cancellationToken);
    }

    public void Remove(MessageReaction reaction)
    {
        _context.MessageReactions.Remove(reaction);
    }

    public async Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
            return Array.Empty<MessageReaction>();

        return await _context.MessageReactions
            .AsNoTracking()
            .Where(reaction => messageIds.Contains(reaction.MessageId))
            .ToListAsync(cancellationToken);
    }
}
