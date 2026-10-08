using System.Security.Claims;
using Application.Common.Interfaces;

namespace Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User
                    .FindFirstValue("sub");

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException(
                    "User identity is missing.");
            }

            return parsedUserId;
        }
    }
}