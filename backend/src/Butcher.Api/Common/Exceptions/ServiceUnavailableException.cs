namespace Butcher.Api.Common.Exceptions;

/// <summary>Un service extérieur ne répond pas (assistant vocal : Mistral) ; l'application, elle, fonctionne.</summary>
public class ServiceUnavailableException(string message) : Exception(message);
