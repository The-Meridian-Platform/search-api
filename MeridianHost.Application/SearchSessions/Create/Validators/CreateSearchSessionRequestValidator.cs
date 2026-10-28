using FluentValidation;
using MeridianHost.Application.SearchSessions.Create.Contracts;

namespace MeridianHost.Application.SearchSessions.Create.Validators;

public class CreateSearchSessionRequestValidator : AbstractValidator<CreateSearchSessionRequest>
{
    public CreateSearchSessionRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .Length(3, 256)
            .WithMessage("Value cannot be empty");

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .When(x => x.TargetType.HasValue)
            .WithMessage("Target type is invalid.");
    }
}