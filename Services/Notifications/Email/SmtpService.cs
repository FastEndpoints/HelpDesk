using System.Net;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Email;

/// <summary>
/// SMTP email service that sends emails using MailKit with connection reuse.
/// </summary>
public sealed partial class SmtpService(IOptions<NotificationSettings> settings, ILogger<SmtpService> logger) : IEmailSender, IAsyncDisposable
{
    readonly SmtpSettings _settings = settings.Value.Smtp;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly CancellationTokenSource _shutdown = new();
    readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);
    SmtpClient? _client;
    Task? _idleDisconnectTask;
    DateTime _lastUsedUtc;
    bool _disposed;

    /// <summary>
    /// Sends an email, reusing the SMTP connection while the service is active.
    /// </summary>
    /// <param name="msg">The email message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SendEmailAsync(EmailMessage msg, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var sender = new MailboxAddress(_settings.SenderName, _settings.SenderEmail);
            var mimeMessage = CreateMimeMessage(sender, msg);

            try
            {
                await SendConnectedAsync(mimeMessage, ct);
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                logger.LogWarning(x, "SMTP delivery failed for {ToEmail}; reconnecting and retrying once", msg.ToEmail);
                await ReconnectAsync(ct);
                await SendConnectedAsync(mimeMessage, ct);
            }

            _lastUsedUtc = DateTime.UtcNow;
            _idleDisconnectTask ??= DisconnectAfterIdleAsync();
        }
        catch (Exception x)
        {
            logger.LogError(x, "Sending email failed for {ToEmail}", msg.ToEmail);

            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_client?.IsConnected is true)
            return;

        _client?.Dispose();
        _client = new();

        var socketOptions = GetSocketOptions();
        await _client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);
        await _client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
    }

    async Task SendConnectedAsync(MimeMessage mimeMessage, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);
        await _client!.SendAsync(mimeMessage, ct);
    }

    async Task ReconnectAsync(CancellationToken ct)
    {
        await DisconnectClientAsync(quit: false, ct);
        await EnsureConnectedAsync(ct);
    }

    async Task DisconnectAfterIdleAsync()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                await Task.Delay(_idleTimeout, _shutdown.Token);
                await _gate.WaitAsync(_shutdown.Token);

                try
                {
                    if (_client?.IsConnected is not true)
                    {
                        _idleDisconnectTask = null;

                        return;
                    }

                    var idleFor = DateTime.UtcNow - _lastUsedUtc;

                    if (idleFor < _idleTimeout)
                        continue;

                    await DisconnectClientAsync(quit: true, CancellationToken.None);
                    _idleDisconnectTask = null;

                    return;
                }
                finally
                {
                    _gate.Release();
                }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
    }

    async Task DisconnectClientAsync(bool quit, CancellationToken ct)
    {
        if (_client is null)
            return;

        try
        {
            if (_client.IsConnected)
                await _client.DisconnectAsync(quit, ct);
        }
        finally
        {
            _client.Dispose();
            _client = null;
        }
    }

    SecureSocketOptions GetSocketOptions()
    {
        if (!_settings.UseSsl)
            return SecureSocketOptions.None;

        return _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _shutdown.CancelAsync();
        await _gate.WaitAsync();

        try
        {
            await DisconnectClientAsync(quit: true, CancellationToken.None);
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
            _shutdown.Dispose();
        }
    }

    static MimeMessage CreateMimeMessage(MailboxAddress sender, EmailMessage msg)
    {
        var processedHtml = ProcessMergeFields(msg.HtmlTemplate, msg.MergeFields);

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(sender);
        mimeMessage.ReplyTo.Add(new MailboxAddress(sender.Name, sender.Address));
        mimeMessage.To.Add(new MailboxAddress(msg.ToName, msg.ToEmail));
        mimeMessage.Subject = msg.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = processedHtml };

        foreach (var attachment in msg.Attachments)
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, GetContentType(attachment.ContentType));

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    static ContentType GetContentType(string contentType)
        => ContentType.TryParse(contentType, out var parsed)
               ? parsed
               : new("application", "octet-stream");

    static string ProcessMergeFields(string template, Dictionary<string, string> mergeFields)
    {
        var templateFields = MergeFieldRegex()
                             .Matches(template)
                             .Select(m => m.Groups[1].Value)
                             .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suppliedFields = mergeFields.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingInData = templateFields.Except(suppliedFields, StringComparer.OrdinalIgnoreCase).ToList();
        var extraInData = suppliedFields.Except(templateFields, StringComparer.OrdinalIgnoreCase).ToList();

        if (missingInData.Count > 0 || extraInData.Count > 0)
        {
            var errors = new List<string>();

            if (missingInData.Count > 0)
                errors.Add($"Template contains markers without matching data: {string.Join(", ", missingInData)}");

            if (extraInData.Count > 0)
                errors.Add($"Merge data contains fields not in template: {string.Join(", ", extraInData)}");

            throw new InvalidOperationException(string.Join("; ", errors));
        }

        if (templateFields.Count == 0)
            return template;

        return MergeFieldRegex().Replace(
            template,
            match => WebUtility.HtmlEncode(mergeFields.First(kv => string.Equals(kv.Key, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase)).Value));
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex MergeFieldRegex();
}