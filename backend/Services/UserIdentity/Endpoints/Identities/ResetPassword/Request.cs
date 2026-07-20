using FluentValidation;

namespace Identities.ResetPassword;

sealed class Request
{
    public string ResetCode { get; set; } = null!;
    public string Password { get; set; } = null!;

    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ResetCode)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(12)
                .MaximumLength(128);
        }
    }
}
