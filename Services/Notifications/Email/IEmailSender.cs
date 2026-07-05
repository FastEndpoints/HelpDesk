namespace Email;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessage msg, CancellationToken ct = default);
}
