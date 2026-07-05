namespace Email;

/// <summary>
/// Represents an email message with merge field support.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// The recipient's display name.
    /// </summary>
    public required string ToName { get; set; }

    /// <summary>
    /// The recipient's email address.
    /// </summary>
    public required string ToEmail { get; set; }

    /// <summary>
    /// The email subject line.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// The HTML message template with {{xyz}} style merge fields.
    /// </summary>
    public required string HtmlTemplate { get; set; }

    /// <summary>
    /// Key/value pairs for merge field replacement. Keys should match field names without braces.
    /// </summary>
    public Dictionary<string, string> MergeFields { get; set; } = new();

    /// <summary>
    /// Files to attach to the email.
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = [];
}

public sealed class EmailAttachment
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
}
