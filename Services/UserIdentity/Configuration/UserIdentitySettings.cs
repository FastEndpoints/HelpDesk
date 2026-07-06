namespace Configuration;

/// <summary>
/// Configuration settings for the UserIdentity service.
/// </summary>
public sealed class UserIdentitySettings
{
    /// <summary>
    /// UserIdentity service-specific configuration.
    /// </summary>
    public UserIdentityServiceSettings UserIdentity { get; set; } = new();

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

    public sealed class UserIdentityServiceSettings
    {
        /// <summary>
        /// The HTTP port used for local endpoint hosting.
        /// </summary>
        public int HttpPort { get; set; } = 5000;

        /// <summary>
        /// The MongoDB database name.
        /// </summary>
        public string DatabaseName { get; set; } = "HelpDesk_UserIdentity";

        /// <summary>
        /// JWT token configuration.
        /// </summary>
        public JwtSettings Jwt { get; set; } = new();
    }

    public sealed class JwtSettings
    {
        /// <summary>
        /// The JWT issuer.
        /// </summary>
        public string Issuer { get; set; } = "HelpDesk.UserIdentity";

        /// <summary>
        /// The JWT audience.
        /// </summary>
        public string Audience { get; set; } = "HelpDesk.Services";

        /// <summary>
        /// Number of days before access tokens expire.
        /// </summary>
        public int AccessTokenDays { get; set; } = 7;

        /// <summary>
        /// The PEM encoded RSA private key used for signing JWTs.
        /// </summary>
        public string PrivateKeyPem { get; set; } = string.Empty;
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
