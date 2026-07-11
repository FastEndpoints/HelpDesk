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
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS for the connection.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// The SMTP authentication username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The SMTP authentication password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The sender's display name.
    /// </summary>
    public string SenderName { get; set; } = "HelpDesk";

    /// <summary>
    /// The sender's email address.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// The name of the administrator.
    /// </summary>
    public string AdminName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the administrator.
    /// </summary>
    public string AdminEmail { get; set; } = string.Empty;
}
