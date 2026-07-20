using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.VerificationIssued;

sealed class UserIdentityVerificationIssuedEventHandler(IDisplayNameStore displayNames)
    : IEventHandler<UserIdentityVerificationIssuedEvent>
{
    public async Task HandleAsync(UserIdentityVerificationIssuedEvent eventModel, CancellationToken ct)
    {
        var displayName = await DisplayName.ResolveAsync(
            displayNames,
            eventModel.UserIdentityId,
            eventModel.Email,
            ct);

        await new SendEmailCommand
        {
            IdempotencyKey = JobIdempotencyKey(eventModel.UserIdentityId, eventModel.VerificationCode),
            Message = new()
            {
                ToEmail = eventModel.Email,
                ToName = displayName,
                Subject = "Welcome to HelpDesk",
                HtmlTemplate = EmailTemplate.Welcome,
                MergeFields = new()
                {
                    ["DisplayName"] = displayName,
                    ["VerificationLink"] = GetVerificationLink(eventModel.BaseUrl, eventModel.VerificationCode)
                }
            }
        }.QueueJobAsync(ct: ct);
    }

    /// <summary>
    /// Job-queue key so duplicate VerificationIssued deliveries enqueue once per identity+code,
    /// while a resend with a new code can queue another email.
    /// </summary>
    internal static string JobIdempotencyKey(string userIdentityId, string verificationCode)
        => $"user-identity-verification:{userIdentityId}:{verificationCode}";

    static string GetVerificationLink(string baseUrl, string verificationCode)
        => $"{baseUrl.TrimEnd('/')}/verify/{Uri.EscapeDataString(verificationCode)}";
}