using FluentValidation;

namespace Application.Features.Spaces.Commands.AddSpaceMember;

public sealed class AddSpaceMemberCommandValidator
    : AbstractValidator<AddSpaceMemberCommand>
{
    public AddSpaceMemberCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}