namespace Butcher.Api.Common;

/// <summary>
/// Heure de l'activité : les jours et les mois des filtres et des rapports sont ceux de
/// <c>Europe/Paris</c>, pas ceux de l'UTC du serveur (specs/005-backoffice, data-model §4).
/// </summary>
/// <remarks>
/// Une vente saisie le 13 à 0 h 30 appartient au 13, alors que son horodatage UTC tombe le 12. Le
/// fuseau est fixé plutôt que lu sur la machine : le conteneur tourne en UTC.
/// </remarks>
public static class BusinessTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    /// <summary>Instant où commence <paramref name="day"/> à Paris.</summary>
    public static DateTimeOffset StartOfDay(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>Jour, à Paris, de l'instant <paramref name="instant"/>.</summary>
    public static DateOnly DayOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
}
