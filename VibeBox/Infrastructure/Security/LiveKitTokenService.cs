using Application.Common.Interfaces;
using Application.Common.Settings;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

public sealed class LiveKitTokenService : ILiveKitTokenService
{
    private readonly LiveKitSettings _settings;

    public LiveKitTokenService(IOptions<LiveKitSettings> options)
    {
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("LiveKit API credentials are not configured.");
    }

    public string CreateToken(Guid roomId, Guid userId, string participantName)
    {
        return new AccessToken(_settings.ApiKey, _settings.ApiSecret)
            .WithIdentity(userId.ToString())
            .WithName(participantName)
            .WithGrants(new VideoGrants
            {
                RoomJoin = true,
                Room = roomId.ToString()
            })
            .WithTtl(TimeSpan.FromHours(1))
            .ToJwt();
    }
}