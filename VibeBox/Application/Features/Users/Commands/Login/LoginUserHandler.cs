using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Users.Commands.Login;

public sealed class LoginUserHandler
    : IRequestHandler<LoginUserCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null ||
            !_passwordHasher.Verify(
                request.Password,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var now = DateTime.UtcNow;

        var accessToken = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken();
        var refreshTokenHash =
            _tokenService.HashRefreshToken(refreshToken);

        var refreshTokenExpiresAt = now.AddDays(
            _jwtSettings.RefreshTokenLifetimeDays);

        var entity = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            refreshTokenHash,
            refreshTokenExpiresAt,
            now);

        await _refreshTokenRepository.AddAsync(
            entity,
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshToken,
            now.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
            refreshTokenExpiresAt);
    }
}