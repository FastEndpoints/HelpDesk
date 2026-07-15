using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.VerificationIssued;

public sealed class UserIdentityVerificationIssuedEventHandler
    : IEventHandler<UserIdentityVerificationIssuedEvent>
{
    public Task HandleAsync(UserIdentityVerificationIssuedEvent eventModel, CancellationToken ct)
    {
        var displayName = GetDefaultDisplayName(eventModel.Email);

        return new SendEmailCommand
        {
            IdempotencyKey = JobIdempotencyKey(eventModel.UserIdentityId),
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

    /// <summary>Job-queue key so duplicate VerificationIssued deliveries enqueue once per identity.</summary>
    internal static string JobIdempotencyKey(string userIdentityId)
        => $"user-identity-verification:{userIdentityId}";

    static string GetDefaultDisplayName(string email)
    {
        var trimmedEmail = email.Trim();
        var atIndex = trimmedEmail.IndexOf('@');

        return atIndex > 0
                   ? trimmedEmail[..atIndex]
                   : trimmedEmail;
    }

    static string GetVerificationLink(string baseUrl, string verificationCode)
        => $"{baseUrl.TrimEnd('/')}/verify/{Uri.EscapeDataString(verificationCode)}";
}
