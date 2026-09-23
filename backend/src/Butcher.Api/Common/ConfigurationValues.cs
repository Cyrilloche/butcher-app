namespace Butcher.Api.Common;

public static class ConfigurationValues
{
    /// <summary>
    /// Valeur d'un réglage, ou <paramref name="fallback"/> s'il est absent <b>ou vide</b> : Docker Compose
    /// transmet une variable non renseignée (<c>${VAR:-}</c>) comme une chaîne vide, pas comme une absence.
    /// </summary>
    public static string ValueOr(this IConfiguration configuration, string key, string fallback) =>
        configuration[key] is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
}
