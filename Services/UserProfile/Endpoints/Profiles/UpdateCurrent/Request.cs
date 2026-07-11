using FluentValidation;

namespace Endpoints.Profiles.UpdateCurrent;

sealed class Request
{
    public string DisplayName { get; set; } = null!;

    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
