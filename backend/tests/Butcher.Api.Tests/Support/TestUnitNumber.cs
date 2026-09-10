namespace Butcher.Api.Tests.Support;

/// <summary>
/// Fournit des numéros d'unité distincts aux unités semées directement en base par les tests.
/// </summary>
/// <remarks>
/// Le numéro porte un index unique : deux unités semées à la main ne peuvent pas partager le même.
/// Les tests qui portent sur la numérotation elle-même passent par le service, jamais par ce
/// générateur, dont les valeurs n'ont volontairement pas la forme d'un vrai numéro d'étiquette.
/// </remarks>
public static class TestUnitNumber
{
    private static int counter;

    public static string Next() => $"TEST-{Interlocked.Increment(ref counter):D6}";
}
