using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Conversations.Commands.RemoveConversationMember;

public sealed class RemoveConversationMemberCommandHandler
    : IRequestHandler<RemoveConversationMemberCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public RemoveConversationMemberCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        RemoveConversationMemberCommand request,
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
            throw new ConflictException("Members can only be removed from group conversations.");

        if (request.UserId == currentUserId)
            throw new ConflictException("You cannot remove yourself. Use the leave group endpoint.");

        conversation.RemoveMember(request.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return request.UserId;
    }
}