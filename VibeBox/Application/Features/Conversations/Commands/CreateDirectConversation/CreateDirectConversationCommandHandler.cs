using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Conversations.Commands.CreateDirectConversation;

public sealed class CreateDirectConversationCommandHandler
    : IRequestHandler<CreateDirectConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDirectConversationCommandHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateDirectConversationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        if (currentUserId == request.UserId)
            throw new ConflictException("You cannot create a conversation with yourself.");

        var targetUser = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (targetUser is null)
            throw new KeyNotFoundException("User not found.");

        var existingConversation = await _conversationRepository.GetDirectAsync(
            currentUserId,
            request.UserId,
            cancellationToken);

        if (existingConversation is not null)
            return existingConversation.Id;

        var now = DateTime.UtcNow;

        var conversation = Conversation.CreateDirect(
            Guid.NewGuid(),
            currentUserId,
            request.UserId,
            now);

        conversation.AddMember(new ConversationMember(
            conversation.Id,
            currentUserId,
            now));

        conversation.AddMember(new ConversationMember(
            conversation.Id,
            request.UserId,
            now));

        return await _conversationRepository.CreateDirectIfNotExistsAsync(
            conversation,
            cancellationToken);
    }
}