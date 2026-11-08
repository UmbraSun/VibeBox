using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Conversations.Commands.LeaveConversation;

public sealed class LeaveConversationCommandHandler
    : IRequestHandler<LeaveConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public LeaveConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        LeaveConversationCommand request,
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
            throw new ConflictException("You can only leave group conversations.");

        conversation.Leave(currentUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return currentUserId;
    }
}