namespace Configuration;

/// <summary>
/// Configuration settings for the Notifications service.
/// </summary>
public sealed class NotificationSettings
{
    /// <summary>
    /// Notifications service-specific configuration.
    /// </summary>
    public NotificationServiceSettings Notifications { get; set; } = new();

    /// <summary>
    /// SMTP email configuration.
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>
    /// Connection string configuration.
    /// </summary>
    public ConnectionStringsSettings ConnectionStrings { get; set; } = new();

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Allowed host configuration.
    /// </summary>
    public string AllowedHosts { get; set; } = "*";

    public sealed class NotificationServiceSettings
    {
        /// <summary>
        /// The MongoDB database name.
        /// </summary>
        public string DatabaseName { get; set; } = "HelpDesk_Notifications";
    }

    public sealed class ConnectionStringsSettings
    {
        /// <summary>
        /// The MongoDB connection string.
        /// </summary>
        public string MongoDB { get; set; } = "mongodb://localhost:27017";
    }

    public sealed class LoggingSettings
    {
        /// <summary>
        /// Log levels by category name.
        /// </summary>
        public Dictionary<string, string> LogLevel { get; set; } = new()
        {
            ["Default"] = "Information",
            ["Microsoft.AspNetCore"] = "Warning"
        };
    }
}
