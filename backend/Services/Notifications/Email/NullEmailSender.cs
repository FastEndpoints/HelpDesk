namespace Email;

public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(EmailMessage msg, CancellationToken ct = default)
    {
        if (msg.MergeFields.TryGetValue("VerificationLink", out var verificationLink))
        {
            logger.LogInformation(
                "Email suppressed. To={ToEmail}, Subject={Subject}, VerificationLink={VerificationLink}",
                msg.ToEmail,
                msg.Subject,
                verificationLink);
        }
        else if (msg.MergeFields.TryGetValue("ResetLink", out var resetLink))
        {
            logger.LogInformation(
                "Email suppressed. To={ToEmail}, Subject={Subject}, ResetLink={ResetLink}",
                msg.ToEmail,
                msg.Subject,
                resetLink);
        }
        else
        {
            logger.LogInformation("Email suppressed. To={ToEmail}, Subject={Subject}", msg.ToEmail, msg.Subject);
        }

        return Task.CompletedTask;
    }
}
