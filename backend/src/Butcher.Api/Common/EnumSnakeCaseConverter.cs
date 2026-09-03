using System.Text.Json;

namespace Butcher.Api.Common;

// Convertit les enums en snake_case pour le stockage EF Core, avec la même politique de casse
// (JsonNamingPolicy.SnakeCaseLower) que celle utilisée pour la sérialisation JSON de l'API
// (Program.cs) — une seule règle de casse, appliquée aux deux endroits.
public static class EnumSnakeCaseConverter
{
    public static string ToSnakeCase<TEnum>(TEnum value) where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    public static TEnum FromSnakeCase<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().First(candidate => ToSnakeCase(candidate) == value);
}
