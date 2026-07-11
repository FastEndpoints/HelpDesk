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

        /// <summary>
        /// JWT token validation configuration shared with UserIdentity token issuance.
        /// </summary>
        public JwtSettings Jwt { get; set; } = new();

        /// <summary>
        /// Local profile picture storage and public URL settings.
        /// </summary>
        public ProfilePictureSettings ProfilePictures { get; set; } = new();
    }

    public sealed class ProfilePictureSettings
    {
        public const long MultipartOverheadBytes = 512 * 1024;

        /// <summary>
        /// Filesystem root for stored profile pictures (relative paths resolve against content root).
        /// </summary>
        public string StorageRoot { get; set; } = "data/profile-pictures";

        /// <summary>
        /// Optional absolute public base for picture URLs (CDN/proxy). When empty, URLs are built from the current request host + /profile-pictures.
        /// </summary>
        public string? PublicBaseUrl { get; set; }

        /// <summary>
        /// Maximum accepted upload size in bytes (default 5 MB).
        /// </summary>
        public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
    }

    public sealed class JwtSettings
    {
        /// <summary>
        /// The expected JWT issuer.
        /// </summary>
        public string Issuer { get; set; } = "HelpDesk.UserIdentity";

        /// <summary>
        /// The expected JWT audience.
        /// </summary>
        public string Audience { get; set; } = "HelpDesk.Services";

        /// <summary>
        /// PKCS#1 RSA private key used by tests to issue JWTs matching the configured public key.
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Base64-encoded RSA public key used to validate JWTs issued by UserIdentity.
        /// </summary>
        public string PublicKey { get; set; } = string.Empty;
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