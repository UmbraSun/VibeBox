using FluentValidation;

namespace Application.Features.Rooms.Commands.RemoveParticipant;

public sealed class RemoveRoomParticipantValidator
    : AbstractValidator<RemoveRoomParticipantCommand>
{
    public RemoveRoomParticipantValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}