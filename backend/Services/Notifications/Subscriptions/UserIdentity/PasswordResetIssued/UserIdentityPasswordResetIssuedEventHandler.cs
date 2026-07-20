using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.PasswordResetIssued;

sealed class UserIdentityPasswordResetIssuedEventHandler(IDisplayNameStore displayNames)
    : IEventHandler<UserIdentityPasswordResetIssuedEvent>
{
    public async Task HandleAsync(UserIdentityPasswordResetIssuedEvent eventModel, CancellationToken ct)
    {
        var displayName = await DisplayName.ResolveAsync(
            displayNames,
            eventModel.UserIdentityId,
            eventModel.Email,
            ct);

        await new SendEmailCommand
        {
            IdempotencyKey = JobIdempotencyKey(eventModel.UserIdentityId, eventModel.ResetCode),
            Message = new()
            {
                ToEmail = eventModel.Email,
                ToName = displayName,
                Subject = "Reset your HelpDesk password",
                HtmlTemplate = EmailTemplate.PasswordReset,
                MergeFields = new()
                {
                    ["DisplayName"] = displayName,
                    ["ResetLink"] = GetResetLink(eventModel.BaseUrl, eventModel.ResetCode)
                }
            }
        }.QueueJobAsync(ct: ct);
    }

    /// <summary>
    /// Job-queue key so duplicate PasswordResetIssued deliveries enqueue once per identity+code,
    /// while a re-request with a new code can queue another email.
    /// </summary>
    internal static string JobIdempotencyKey(string userIdentityId, string resetCode)
        => $"user-identity-password-reset:{userIdentityId}:{resetCode}";

    static string GetResetLink(string baseUrl, string resetCode)
        => $"{baseUrl.TrimEnd('/')}/reset-password/{Uri.EscapeDataString(resetCode)}";
}
