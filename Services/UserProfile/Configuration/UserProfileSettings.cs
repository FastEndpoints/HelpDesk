namespace Configuration;

/// <summary>
/// Configuration settings for the UserProfile service.
/// </summary>
public sealed class UserProfileSettings
{
    /// <summary>
    /// UserProfile service-specific configuration.
    /// </summary>
    public UserProfileServiceSettings UserProfile { get; set; } = new();

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

    public sealed class UserProfileServiceSettings
    {
        /// <summary>
        /// The HTTP port used for local endpoint hosting.
        /// </summary>
        public int HttpPort { get; set; } = 5001;

        /// <summary>
        /// The MongoDB database name.
        /// </summary>
        public string DatabaseName { get; set; } = "HelpDesk_UserProfile";
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
