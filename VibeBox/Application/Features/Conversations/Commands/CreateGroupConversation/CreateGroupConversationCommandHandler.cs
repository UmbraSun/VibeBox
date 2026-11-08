using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Conversations.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandler
    : IRequestHandler<CreateGroupConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateGroupConversationCommandHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ConflictException("A group name is required.");

        var name = request.Name.Trim();

        if (name.Length > 200)
            throw new ConflictException("The group name cannot exceed 200 characters.");

        if (request.UserIds.Count < 2)
            throw new ConflictException(
                "A group conversation requires at least two other participants.");

        if (request.UserIds.Distinct().Count() != request.UserIds.Count)
            throw new ConflictException(
                "The participant list contains duplicate users.");

        if (request.UserIds.Contains(currentUserId))
            throw new ConflictException(
                "You cannot add yourself to the participant list.");

        foreach (var userId in request.UserIds)
        {
            var user = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
                throw new KeyNotFoundException($"User '{userId}' was not found.");
        }

        var now = DateTime.UtcNow;

        var conversation = Conversation.CreateGroup(
            Guid.NewGuid(),
            name,
            now);

        conversation.AddMember(new ConversationMember(
            conversation.Id,
            currentUserId,
            now));

        foreach (var userId in request.UserIds)
        {
            conversation.AddMember(new ConversationMember(
                conversation.Id,
                userId,
                now));
        }

        return await _conversationRepository.CreateGroupAsync(
            conversation,
            cancellationToken);
    }
}