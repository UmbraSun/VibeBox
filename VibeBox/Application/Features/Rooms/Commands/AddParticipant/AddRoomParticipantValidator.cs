using FluentValidation;

namespace Application.Features.Rooms.Commands.AddParticipant;

public sealed class AddRoomParticipantValidator
    : AbstractValidator<AddRoomParticipantCommand>
{
    public AddRoomParticipantValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}