using FluentValidation;

namespace Application.Features.Users.Commands.Login;

public sealed class LoginUserValidator
    : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}