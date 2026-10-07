namespace Application.Common.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = null!;

    public string Audience { get; init; } = null!;

    public string SecretKey { get; init; } = null!;

    public int AccessTokenLifetimeMinutes { get; init; }

    public int RefreshTokenLifetimeDays { get; init; }
}