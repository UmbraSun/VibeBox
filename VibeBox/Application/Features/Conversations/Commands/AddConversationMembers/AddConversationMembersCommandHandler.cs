using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Conversations.Commands.AddConversationMembers;

public sealed class AddConversationMembersCommandHandler
    : IRequestHandler<AddConversationMembersCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public AddConversationMembersCommandHandler(
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task Handle(
        AddConversationMembersCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            throw new KeyNotFoundException("Conversation not found.");

        if (conversation.Type != ConversationType.Group)
            throw new ConflictException("Members can only be added to group conversations.");

        if (request.UserIds.Count == 0)
            throw new ConflictException("At least one participant is required.");

        if (request.UserIds.Distinct().Count() != request.UserIds.Count)
            throw new ConflictException("The participant list contains duplicate users.");

        foreach (var userId in request.UserIds)
        {
            if (conversation.Members.Any(member => member.UserId == userId))
                throw new ConflictException($"User '{userId}' is already a member of this conversation.");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new KeyNotFoundException($"User '{userId}' was not found.");

            conversation.AddMember(new ConversationMember(
                conversation.Id,
                userId,
                DateTime.UtcNow));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}