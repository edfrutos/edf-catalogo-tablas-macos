namespace EDFCatalogoTablasNet;

/// <summary>
/// Roles en MongoDB pueden venir como "admin"/"Admin"; la app usa forma canónica para filtros y UI.
/// </summary>
public static class RoleHelper
{
    public static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "User";
        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return "Admin";
        if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
            return "User";
        if (role.Equals("guest", StringComparison.OrdinalIgnoreCase))
            return "Guest";
        return role;
    }

    public static bool IsAdmin(string? role) =>
        !string.IsNullOrEmpty(role) &&
        role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}
