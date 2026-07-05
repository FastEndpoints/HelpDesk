using FluentValidation;

namespace Endpoints.Identities.Login;

sealed class Request
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MaximumLength(128);
        }
    }
}