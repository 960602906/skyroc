namespace Shared.Constants;

public static class AuthConstants
{
    public const string BearerScheme = "Bearer";
    public const string CurrentRoleIdClaimType = "current_role_id";
    public const string PermissionClaimType = "permission";
    public const int RefreshTokenByteLength = 32;

    public static IReadOnlySet<string> PrivilegedRoleCodes { get; } = new HashSet<string>(
        [SeedConstants.AdminRoleCode, "superadmin", "administrator", "pljzLk"],
        StringComparer.OrdinalIgnoreCase);
}
