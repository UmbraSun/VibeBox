using FluentValidation;

namespace Application.Features.Users.Commands.Refresh;

public sealed class RefreshAccessTokenValidator
    : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}