using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Settings;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Channels.Commands.CreateLiveKitToken;

public sealed class CreateLiveKitTokenCommandHandler
    : IRequestHandler<CreateLiveKitTokenCommand, LiveKitTokenResult>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILiveKitTokenService _liveKitTokenService;
    private readonly LiveKitSettings _liveKitSettings;

    public CreateLiveKitTokenCommandHandler(
        ISpaceRepository spaceRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILiveKitTokenService liveKitTokenService,
        IOptions<LiveKitSettings> liveKitSettings)
    {
        _spaceRepository = spaceRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _liveKitTokenService = liveKitTokenService;
        _liveKitSettings = liveKitSettings.Value;
    }

    public async Task<LiveKitTokenResult> Handle(
        CreateLiveKitTokenCommand request,
        CancellationToken cancellationToken)
    {
        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
            throw new KeyNotFoundException("Channel was not found.");

        if (channel.Type != ChannelType.Voice)
            throw new ConflictException(
                "LiveKit tokens can only be issued for voice channels.");

        var userId = _currentUserService.UserId;

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            userId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException(
                "You are not a member of this space.");

        var user = await _userRepository.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User was not found.");

        var participantName = user.Email.Split('@')[0];

        var token = _liveKitTokenService.CreateToken(
            channel.Id,
            userId,
            participantName);

        return new LiveKitTokenResult(
            _liveKitSettings.Url,
            token,
            channel.Id.ToString());
    }
}