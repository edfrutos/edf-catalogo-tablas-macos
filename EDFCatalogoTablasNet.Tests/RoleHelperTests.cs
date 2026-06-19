using EDFCatalogoTablasNet;

namespace EDFCatalogoTablasNet.Tests;

public sealed class RoleHelperTests
{
    public static TheoryData<string?, string> NormalizeCases => new()
    {
        { null, "User" },
        { "", "User" },
        { "   ", "User" },
        { "admin", "Admin" },
        { "ADMIN", "Admin" },
        { "user", "User" },
        { "USER", "User" },
        { "guest", "Guest" },
        { "CustomRole", "CustomRole" }
    };

    [Theory]
    [MemberData(nameof(NormalizeCases))]
    public void NormalizeRole_maps_expected(string? input, string expected) =>
        Assert.Equal(expected, RoleHelper.NormalizeRole(input));

    public static TheoryData<string?, bool> IsAdminCases => new()
    {
        { null, false },
        { "", false },
        { "user", false },
        { "Admin", true },
        { "admin", true },
        { "ADMIN", true }
    };

    [Theory]
    [MemberData(nameof(IsAdminCases))]
    public void IsAdmin_is_case_insensitive_for_Admin(string? role, bool expected) =>
        Assert.Equal(expected, RoleHelper.IsAdmin(role));
}
