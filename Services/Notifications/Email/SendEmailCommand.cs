namespace Email;

public sealed class SendEmailCommand : ICommand
{
    public required EmailMessage Message { get; set; }
}

public sealed class SendEmailCommandHandler(IEmailSender emailSender) : ICommandHandler<SendEmailCommand>
{
    public Task ExecuteAsync(SendEmailCommand command, CancellationToken ct)
        => emailSender.SendEmailAsync(command.Message, ct);
}
