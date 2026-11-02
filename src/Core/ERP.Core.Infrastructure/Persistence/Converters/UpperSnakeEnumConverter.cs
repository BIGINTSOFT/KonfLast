using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP.Core.Infrastructure.Persistence.Converters;

/// <summary>Enum üyesini BÜYÜK_SNAKE_CASE metne çevirir: InProduction ↔ IN_PRODUCTION.</summary>
public sealed class UpperSnakeEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => UpperSnakeEnumConverter.ToDbValue(value.ToString()),
    text => Enum.Parse<TEnum>(text.Replace("_", string.Empty), true))
    where TEnum : struct, Enum;

public static class UpperSnakeEnumConverter
{
    public static ValueConverter Create(Type enumType) =>
        (ValueConverter)Activator.CreateInstance(typeof(UpperSnakeEnumConverter<>).MakeGenericType(enumType))!;

    public static string ToDbValue(string enumMemberName) =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(enumMemberName);

    /// <summary>Check constraint için: [Column] IN ('A','B',...)</summary>
    public static string CheckConstraintSql<TEnum>(string columnName) where TEnum : struct, Enum =>
        $"[{columnName}] IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{ToDbValue(n)}'"))})";
}
