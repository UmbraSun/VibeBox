using Domain.Enums;

namespace Application.Common.Models;

public sealed record CreateChannelRequest(
    string Name,
    ChannelType Type);