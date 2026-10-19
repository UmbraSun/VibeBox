using MediatR;

namespace Application.Features.Spaces.Commands.CreateSpace;

public sealed record CreateSpaceCommand(string Name) : IRequest<Guid>;