using FluentValidation;

namespace Application.Features.Spaces.Commands.CreateChannel;

public sealed class CreateChannelCommandValidator
    : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}