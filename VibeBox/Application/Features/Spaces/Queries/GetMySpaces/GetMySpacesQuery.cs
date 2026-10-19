using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetMySpaces;

public sealed record GetMySpacesQuery : IRequest<IReadOnlyList<Space>>;