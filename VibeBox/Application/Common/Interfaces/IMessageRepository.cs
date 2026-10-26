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

    /// <summary>
    /// Asynchronously retrieves a message attachment by its unique identifier.
    /// </summary>
    /// <param name="attachmentId">The unique identifier of the attachment to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the message attachment if found;
    /// otherwise, null.</returns>
    Task<MessageAttachment?> GetAttachmentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken);
}