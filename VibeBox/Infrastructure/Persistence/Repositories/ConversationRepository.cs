using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
    }

    public async Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation => conversation.Id == conversationId,
                cancellationToken);
    }

    public async Task<Conversation?> GetByIdForMemberAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .Include(conversation => conversation.Members)
                .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.Id == conversationId &&
                    conversation.Members.Any(member => member.UserId == userId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Members)
                .ThenInclude(member => member.User)
            .Include(conversation => conversation.Messages
                .OrderByDescending(message => message.CreatedAtUtc)
                .ThenByDescending(message => message.Id)
                .Take(1))
                .ThenInclude(message => message.Attachments)
            .Where(conversation =>
                conversation.Members.Any(member => member.UserId == userId)
                && (conversation.Type == ConversationType.Group
                    || conversation.Messages.Any()))
            .OrderByDescending(conversation =>
                conversation.LastMessageAtUtc ?? conversation.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetDirectAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (firstUserId == secondUserId)
        {
            return null;
        }

        var lowId = firstUserId.CompareTo(secondUserId) < 0
            ? firstUserId
            : secondUserId;

        var highId = firstUserId.CompareTo(secondUserId) < 0
            ? secondUserId
            : firstUserId;

        return await _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.DirectUserLowId == lowId &&
                    conversation.DirectUserHighId == highId,
                cancellationToken);
    }

    public async Task<Conversation?> GetDirectForUpdateAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (firstUserId == secondUserId)
            return null;

        var lowId = firstUserId.CompareTo(secondUserId) < 0
            ? firstUserId
            : secondUserId;

        var highId = firstUserId.CompareTo(secondUserId) < 0
            ? secondUserId
            : firstUserId;

        return await _context.Conversations
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.Type == ConversationType.Direct &&
                    conversation.DirectUserLowId == lowId &&
                    conversation.DirectUserHighId == highId,
                cancellationToken);
    }

    public async Task<Guid> CreateDirectIfNotExistsAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        await _context.Conversations.AddAsync(
            conversation,
            cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return conversation.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_conversations_DirectUserLowId_DirectUserHighId"
            })
        {
            _context.Entry(conversation).State = EntityState.Detached;

            var existingConversation = await GetDirectAsync(
                conversation.DirectUserLowId!.Value,
                conversation.DirectUserHighId!.Value,
                cancellationToken);

            if (existingConversation is not null)
                return existingConversation.Id;

            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _context.ConversationMembers
            .AsNoTracking()
            .Where(member => member.ConversationId == conversationId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateGroupAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        await _context.Conversations.AddAsync(
            conversation,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return conversation.Id;
    }
}