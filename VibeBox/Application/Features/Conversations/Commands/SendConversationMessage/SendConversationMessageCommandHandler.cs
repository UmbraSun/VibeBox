using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Conversations.Commands.SendConversationMessage;

public sealed class SendConversationMessageCommandHandler
    : IRequestHandler<SendConversationMessageCommand, Guid>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public SendConversationMessageCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<Guid> Handle(
        SendConversationMessageCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
        {
            throw new KeyNotFoundException("Conversation was not found.");
        }

        var content = request.Content.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException(
                "Message content cannot be empty.");
        }

        if (request.ReplyToMessageId.HasValue)
        {
            var replyToMessage = await _messageRepository.GetByIdAsync(
                request.ReplyToMessageId.Value,
                cancellationToken);

            if (replyToMessage is null)
                throw new KeyNotFoundException("Reply message was not found.");

            if (replyToMessage.ConversationId != conversation.Id)
            {
                throw new ConflictException(
                    "Reply message must belong to the same conversation.");
            }
        }

        var createdAtUtc = DateTime.UtcNow;

        var message = Message.CreateForConversation(
            Guid.NewGuid(),
            conversation.Id,
            currentUserId,
            content,
            createdAtUtc,
            request.ReplyToMessageId);

        conversation.UpdateLastMessageAt(createdAtUtc);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}