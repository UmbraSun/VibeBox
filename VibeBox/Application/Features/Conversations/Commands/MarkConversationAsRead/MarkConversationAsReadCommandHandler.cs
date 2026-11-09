
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Conversations.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandler
    : IRequestHandler<MarkConversationAsReadCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public MarkConversationAsReadCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task Handle(
        MarkConversationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            userId,
            cancellationToken);

        if (conversation is null)
            throw new KeyNotFoundException("Conversation was not found.");

        var member = conversation.Members.First(
            member => member.UserId == userId);

        member.MarkAsRead(request.ReadAtUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
