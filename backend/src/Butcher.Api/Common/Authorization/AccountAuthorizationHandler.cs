using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Common.Authorization;

/// <summary>
/// Relit le compte en base pour décider (research R-02, R-03) : une désactivation ou une
/// rétrogradation prend effet à la requête suivante, sans attendre l'expiration du jeton.
/// </summary>
/// <remarks>
/// Un refus porte une raison, que <see cref="AccountAuthorizationResultHandler"/> traduit en réponse :
/// compte inconnu ou désactivé → <c>401</c>, pour que le client ferme la session ; rôle insuffisant →
/// <c>403</c>.
/// </remarks>
public sealed class AccountAuthorizationHandler(AppDbContext dbContext) : AuthorizationHandler<AccountRequirement>
{
    public const string InactiveAccountReason = "inactive_account";

    public const string AdminRequiredReason = "admin_required";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AccountRequirement requirement)
    {
        if (AccountClaims.GetAccountId(context.User) is not { } accountId)
        {
            return;
        }

        var account = await dbContext.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == accountId)
            .Select(u => new { u.IsActive, u.Role })
            .SingleOrDefaultAsync();

        if (account is not { IsActive: true })
        {
            context.Fail(new AuthorizationFailureReason(this, InactiveAccountReason));
            return;
        }

        if (requirement.RequireAdmin && account.Role != AccountRole.Admin)
        {
            context.Fail(new AuthorizationFailureReason(this, AdminRequiredReason));
            return;
        }

        context.Succeed(requirement);
    }
}
