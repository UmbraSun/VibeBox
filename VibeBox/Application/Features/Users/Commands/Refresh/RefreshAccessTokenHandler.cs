using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Features.Users.Commands.Login;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Users.Commands.Refresh;

public sealed class RefreshAccessTokenHandler
    : IRequestHandler<RefreshAccessTokenCommand, LoginResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public RefreshAccessTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse> Handle(
        RefreshAccessTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(
            request.RefreshToken);

        var currentToken = await _refreshTokenRepository.GetByHashAsync(
            tokenHash,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (currentToken is null || !currentToken.IsActive(now))
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var newRefreshToken = _tokenService.CreateRefreshToken();

        var newRefreshTokenHash =
            _tokenService.HashRefreshToken(newRefreshToken);

        var newRefreshTokenExpiresAt = now.AddDays(
            _jwtSettings.RefreshTokenLifetimeDays);

        var replacement = new RefreshToken(
            Guid.NewGuid(),
            currentToken.UserId,
            newRefreshTokenHash,
            newRefreshTokenExpiresAt,
            now);

        currentToken.Revoke(
            now,
            newRefreshTokenHash);

        await _refreshTokenRepository.AddAsync(
            replacement,
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(
            currentToken.User);

        return new LoginResponse(
            accessToken,
            newRefreshToken,
            now.AddMinutes(
                _jwtSettings.AccessTokenLifetimeMinutes),
            newRefreshTokenExpiresAt);
    }
}