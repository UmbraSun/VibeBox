using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents a repository for managing messages.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Adds a new message to the repository.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        Message message,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets messages by channel.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="beforeMessageId"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<Message>> GetByChannelAsync(
        Guid channelId,
        Guid? beforeMessageId,
        int limit,
        CancellationToken cancellationToken);
}