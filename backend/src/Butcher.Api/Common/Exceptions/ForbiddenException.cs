namespace Butcher.Api.Common.Exceptions;

/// <summary>Action refusée au compte authentifié, faute de droits (<c>403</c>).</summary>
public class ForbiddenException(string message) : Exception(message);
