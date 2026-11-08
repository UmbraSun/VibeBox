using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Conversations.Commands.RenameConversation;

public sealed class RenameConversationCommandHandler
    : IRequestHandler<RenameConversationCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public RenameConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task Handle(
        RenameConversationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            _currentUserService.UserId,
            cancellationToken);

        if (conversation is null)
            throw new KeyNotFoundException("Conversation not found.");

        if (conversation.Type != ConversationType.Group)
            throw new ConflictException("Only group conversations can be renamed.");

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ConflictException("A group name is required.");

        if (name.Length > 200)
            throw new ConflictException("The group name cannot exceed 200 characters.");

        conversation.Rename(name);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}