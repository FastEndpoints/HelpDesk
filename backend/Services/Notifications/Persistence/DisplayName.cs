namespace Persistence;

static class DisplayName
{
    public static async Task<string> ResolveAsync(IDisplayNameStore store,
                                                  string userIdentityId,
                                                  string email,
                                                  CancellationToken ct)
    {
        var projected = await store.FindAsync(userIdentityId, ct);

        return string.IsNullOrWhiteSpace(projected)
                   ? FromEmail(email)
                   : projected.Trim();
    }

    public static string FromEmail(string email)
    {
        var trimmedEmail = email.Trim();
        var atIndex = trimmedEmail.IndexOf('@');

        return atIndex > 0
                   ? trimmedEmail[..atIndex]
                   : trimmedEmail;
    }
}
