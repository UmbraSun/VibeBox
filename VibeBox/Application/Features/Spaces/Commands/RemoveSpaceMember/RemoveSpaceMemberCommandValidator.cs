using FluentValidation;

namespace Application.Features.Spaces.Commands.RemoveSpaceMember;

public sealed class RemoveSpaceMemberCommandValidator
    : AbstractValidator<RemoveSpaceMemberCommand>
{
    public RemoveSpaceMemberCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}