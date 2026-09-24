using Microsoft.AspNetCore.Authorization;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Exige un compte actif, et en plus administrateur si <see cref="RequireAdmin"/> est vrai, ou
/// titulaire de l'assistant vocal si <see cref="RequireAssistant"/> est vrai (RF-36).
/// </summary>
public sealed class AccountRequirement : IAuthorizationRequirement
{
    public static AccountRequirement Active { get; } = new(requireAdmin: false, requireAssistant: false);

    public static AccountRequirement Administrator { get; } = new(requireAdmin: true, requireAssistant: false);

    public static AccountRequirement AssistantUser { get; } = new(requireAdmin: false, requireAssistant: true);

    private AccountRequirement(bool requireAdmin, bool requireAssistant)
    {
        RequireAdmin = requireAdmin;
        RequireAssistant = requireAssistant;
    }

    public bool RequireAdmin { get; }

    public bool RequireAssistant { get; }
}
