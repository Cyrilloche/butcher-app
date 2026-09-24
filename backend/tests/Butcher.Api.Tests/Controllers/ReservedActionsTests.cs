using System.Reflection;
using Butcher.Api.Common.Authorization;
using Butcher.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Tests.Controllers;

/// <summary>
/// Inventaire des gestes métier réservés à l'administrateur (FR-011, FR-012, clarification du
/// 2026-09-12). Une restriction ajoutée ou retirée par mégarde sur un contrôleur métier fait échouer
/// ce test : la liste ne change que par décision.
/// </summary>
public class ReservedActionsTests
{
    /// <summary>Contrôleurs d'administration, entièrement réservés : hors de l'inventaire métier.</summary>
    private static readonly HashSet<Type> AdministrationControllers =
    [
        typeof(AccountsController),
        typeof(AuditEntriesController),
        typeof(ReportsController),
    ];

    private static readonly string[] ExpectedReservedActions =
    [
        $"{nameof(ProductsController)}.{nameof(ProductsController.Deactivate)}",
        $"{nameof(ProductsController)}.{nameof(ProductsController.Reactivate)}",
        $"{nameof(ProductsController)}.{nameof(ProductsController.WriteOffStock)}",
    ];

    [Fact]
    public void BusinessControllers_ReserveExactlyTheCatalogueWideActions()
    {
        var reserved = typeof(ProductsController).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract && !AdministrationControllers.Contains(t))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(action => IsAdminOnly(controller) || IsAdminOnly(action))
                .Select(action => $"{controller.Name}.{action.Name}"))
            .Order()
            .ToArray();

        Assert.Equal(ExpectedReservedActions.Order(), reserved);
    }

    [Fact]
    public void AdministrationControllers_AreReservedAsAWhole()
    {
        Assert.All(AdministrationControllers, controller => Assert.True(IsAdminOnly(controller)));
    }

    /// <summary>
    /// L'assistant vocal n'est ouvert qu'aux comptes pour lesquels l'administrateur l'a activé (RF-36,
    /// FR-022) : posée sur le contrôleur entier, la politique couvre toute action ajoutée plus tard.
    /// </summary>
    [Fact]
    public void AssistantController_RequiresTheAssistantAsAWhole()
    {
        Assert.Contains(typeof(AssistantController).GetCustomAttributes<AuthorizeAttribute>(inherit: true),
            a => a.Policy == AuthorizationPolicies.AssistantEnabled);
        Assert.DoesNotContain(typeof(AssistantController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(action => action.GetCustomAttributes<AllowAnonymousAttribute>()), _ => true);
    }

    private static bool IsAdminOnly(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Any(a => a.Policy == AuthorizationPolicies.AdminOnly);
}
