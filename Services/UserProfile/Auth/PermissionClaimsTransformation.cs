using System.Security.Claims;
using Common.Tools;
using Microsoft.AspNetCore.Authentication;

namespace Auth;

/// <summary>
/// Expands JWT role claims (permission group names) into local FE permission codes.
/// Identity mints group names only; each resource service maps groups → its generated Allow codes.
/// </summary>
sealed class PermissionClaimsTransformation : IClaimsTransformation
{
    const string PermissionsClaimType = "permissions";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        // Idempotent: leave existing permissions claims alone (e.g. tests that mint codes directly).
        if (identity.HasClaim(c => c.Type == PermissionsClaimType))
            return Task.FromResult(principal);

        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in identity.FindAll(identity.RoleClaimType).Select(c => c.Value))
        {
            // Unknown roles → no codes (fail closed). Multi-role → union.
            // Only map groups that exist on generated Allow (Admin appears after an endpoint uses it).
            var groupCodes = role switch
            {
                PermissionGroups.User => Services.UserProfile.Auth.Allow.User,
                _ => null
            };

            if (groupCodes is null)
                continue;

            foreach (var code in groupCodes)
                codes.Add(code);
        }

        foreach (var code in codes)
            identity.AddClaim(new(PermissionsClaimType, code));

        return Task.FromResult(principal);
    }
}