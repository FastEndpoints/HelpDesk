namespace Email;

public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(EmailMessage msg, CancellationToken ct = default)
    {
        logger.LogInformation("Email suppressed. To={ToEmail}, Subject={Subject}", msg.ToEmail, msg.Subject);

        return Task.CompletedTask;
    }
}
