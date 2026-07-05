using Contracts.UserProfile;

namespace Subscriptions.UserProfile.Registration;

public sealed class UserProfileRegisteredEventHandler
    : IEventHandler<UserProfileRegisteredEvent>
{
    public Task HandleAsync(UserProfileRegisteredEvent eventModel, CancellationToken ct)
        => new SendEmailCommand
        {
            Message = new()
            {
                ToEmail = eventModel.Email,
                ToName = eventModel.DisplayName,
                Subject = "Welcome to HelpDesk",
                HtmlTemplate = EmailTemplate.Welcome,
                MergeFields = new()
                {
                    ["DisplayName"] = eventModel.DisplayName,
                    ["VerificationLink"] = GetVerificationLink(eventModel.BaseUrl, eventModel.VerificationCode)
                }
            }
        }.QueueJobAsync(ct: ct);

    static string GetVerificationLink(string baseUrl, string verificationCode)
        => $"{baseUrl.TrimEnd('/')}/identities/verify/{Uri.EscapeDataString(verificationCode)}";
}
