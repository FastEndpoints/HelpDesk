namespace Email;

/// <summary>
/// SMTP configuration settings loaded from appsettings.json.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// Whether SMTP delivery is enabled. Disabled by default to avoid accidental emails in development and testing.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The SMTP server hostname.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// The SMTP server port.
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS for the connection.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// The SMTP authentication username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The SMTP authentication password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// The sender's display name.
    /// </summary>
    public required string SenderName { get; set; }

    /// <summary>
    /// The sender's email address.
    /// </summary>
    public required string SenderEmail { get; set; }

    /// <summary>
    /// The name of the administrator.
    /// </summary>
    public required string AdminName { get; set; }

    /// <summary>
    /// The email address of the administrator.
    /// </summary>
    public required string AdminEmail { get; set; }
}
