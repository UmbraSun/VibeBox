using FluentValidation;

namespace Application.Features.Rooms.Commands.Create;

public sealed class CreateRoomValidator
    : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}