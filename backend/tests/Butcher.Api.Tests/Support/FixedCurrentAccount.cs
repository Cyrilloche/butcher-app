using Butcher.Api.Common.Authorization;

namespace Butcher.Api.Tests.Support;

/// <summary>Compte courant figé, pour simuler une requête authentifiée sans HTTP.</summary>
public sealed class FixedCurrentAccount(Guid? accountId) : ICurrentAccount
{
    public Guid? AccountId { get; } = accountId;
}
