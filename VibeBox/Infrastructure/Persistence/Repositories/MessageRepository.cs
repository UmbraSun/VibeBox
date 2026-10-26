using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly ApplicationDbContext _context;

    public MessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetByChannelAsync(
        Guid channelId,
        Guid? beforeMessageId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _context.Messages
            .AsNoTracking()
            .Where(message => message.ChannelId == channelId);

        if (beforeMessageId.HasValue)
        {
            var cursor = await _context.Messages
                .AsNoTracking()
                .Where(message =>
                    message.Id == beforeMessageId.Value &&
                    message.ChannelId == channelId)
                .Select(message => new
                {
                    message.CreatedAtUtc,
                    message.Id
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is null)
            {
                return Array.Empty<Message>();
            }

            query = query.Where(message =>
                message.CreatedAtUtc < cursor.CreatedAtUtc ||
                (message.CreatedAtUtc == cursor.CreatedAtUtc &&
                 message.Id.CompareTo(cursor.Id) < 0));
        }

        var messages = await query
            .Include(message => message.Sender)
            .Include(message => message.Attachments)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return messages;
    }

    public async Task<MessageAttachment?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        return await _context.MessageAttachments
            .AsNoTracking()
            .Include(attachment => attachment.Message)
            .ThenInclude(message => message.Channel)
            .FirstOrDefaultAsync(
                attachment => attachment.Id == attachmentId,
                cancellationToken);
    }
}