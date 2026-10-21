using FluentValidation;

namespace Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommandValidator
    : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .MaximumLength(4000);
    }
}