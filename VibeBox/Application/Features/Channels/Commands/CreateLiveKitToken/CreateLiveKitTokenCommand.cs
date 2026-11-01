using Application.Common.Models;
using MediatR;

namespace Application.Features.Channels.Commands.CreateLiveKitToken;

public sealed record CreateLiveKitTokenCommand(Guid ChannelId)
    : IRequest<LiveKitTokenResult>;