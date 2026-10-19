using FluentValidation;

namespace Application.Features.Spaces.Commands.CreateSpace;

public sealed class CreateSpaceCommandValidator
    : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Space name cannot be empty.");
    }
}