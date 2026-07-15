namespace Email;

public sealed class SendEmailCommand : ICommand
{
    /// <summary>
    /// Optional business key for job-queue idempotency. Null/empty/whitespace is not deduped.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    public required EmailMessage Message { get; set; }
}

public sealed class SendEmailCommandHandler(IEmailSender emailSender) : ICommandHandler<SendEmailCommand>
{
    public Task ExecuteAsync(SendEmailCommand command, CancellationToken ct)
        => emailSender.SendEmailAsync(command.Message, ct);
}
