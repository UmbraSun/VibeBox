using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Users.Queries.SearchUsers;

public sealed class SearchUsersHandler
    : IRequestHandler<SearchUsersQuery, IReadOnlyList<SearchUsersResponse>>
{
    private const int MinSearchLength = 3;
    private const int MaxResults = 20;

    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public SearchUsersHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<SearchUsersResponse>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (email.Length < MinSearchLength)
        {
            return [];
        }

        var users = await _userRepository.SearchByEmailAsync(
            email,
            _currentUserService.UserId,
            MaxResults,
            cancellationToken);

        return users
            .Select(user => new SearchUsersResponse(
                user.Id,
                user.Email))
            .ToList();
    }
}