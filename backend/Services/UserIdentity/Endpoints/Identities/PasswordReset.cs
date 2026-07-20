namespace Identities;

/// <summary>Shared password-reset token lifetime and request cooldown policy.</summary>
static class PasswordReset
{
    /// <summary>Response header (seconds) on every successful forgot-password response.</summary>
    internal const string AvailableInHeaderName = "Reset-Available-In";

    internal static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    internal static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(30);

    internal const string RequestSuccessMessage = "If an account exists for that email, we sent a reset link.";
    internal const string ResetSuccessMessage = "Password updated. You can sign in.";
    internal const string InvalidOrExpiredMessage = "Invalid or expired reset link.";

    /// <summary>
    /// Seconds until a new reset email may be issued, from the latest token <paramref name="createdAt" />.
    /// </summary>
    internal static int AvailableInSeconds(DateTime createdAt, DateTime utcNow)
    {
        var remaining = RequestCooldown - (utcNow - createdAt);

        return remaining <= TimeSpan.Zero
                   ? 0
                   : (int)Math.Ceiling(remaining.TotalSeconds);
    }
}