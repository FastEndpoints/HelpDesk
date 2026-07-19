using FluentValidation;

namespace Identities.ResendVerification;

sealed class Request
{
    public string Email { get; set; } = null!;

    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);
        }
    }
}
