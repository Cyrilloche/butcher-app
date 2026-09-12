using Microsoft.AspNetCore.Authorization;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Exige un compte actif, et administrateur si <see cref="RequireAdmin"/> est vrai.
/// </summary>
public sealed class AccountRequirement : IAuthorizationRequirement
{
    public static AccountRequirement Active { get; } = new(requireAdmin: false);

    public static AccountRequirement Administrator { get; } = new(requireAdmin: true);

    private AccountRequirement(bool requireAdmin) => RequireAdmin = requireAdmin;

    public bool RequireAdmin { get; }
}
