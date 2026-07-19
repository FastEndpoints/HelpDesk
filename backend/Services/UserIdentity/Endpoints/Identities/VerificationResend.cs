namespace Identities;

/// <summary>Shared resend-verification cooldown policy (login header + resend gate).</summary>
static class VerificationResend
{
    /// <summary>Response header (seconds) on non-Active login after password validation.</summary>
    internal const string AvailableInHeaderName = "Resend-Available-In";

    internal static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

    internal static int AvailableInSeconds(DateTime verificationIssuedAt, DateTime utcNow)
    {
        var remaining = Cooldown - (utcNow - verificationIssuedAt);

        return remaining <= TimeSpan.Zero
                   ? 0
                   : (int)Math.Ceiling(remaining.TotalSeconds);
    }
}
