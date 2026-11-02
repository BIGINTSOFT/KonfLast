using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ERP.Core.Application.Common;

/// <summary>
/// Liste ekranlarının sütun filtresi ve sıralaması. Filtre ve sıralama, liste satırının (DTO) alan adlarıyla
/// verilir; sorgunun son projeksiyonu (<c>Select(e =&gt; new Dto(...))</c>) okunarak her alanın kaynak ifadesi
/// bulunur ve koşul veritabanı sorgusuna o ifade üzerinden eklenir. Böylece filtre sayfalamadan önce, tüm
/// kayıtlarda uygulanır.
/// </summary>
/// <remarks>
/// Metin alanlarında "içerir" (başına <c>=</c> konursa "eşittir"), sayı / enum / mantıksal / tarih alanlarında
/// "eşittir" uygulanır. Tanınmayan alan, çevrilemeyen değer ya da süzülemeyen tür (liste vb.) yok sayılır.
/// </remarks>
public static class GridQuery
{
    public static IQueryable<T> Apply<T>(IQueryable<T> query, string? sort, IReadOnlyDictionary<string, string>? filters)
    {
        var activeFilters = filters?
            .Where(f => !string.IsNullOrWhiteSpace(f.Key) && !string.IsNullOrWhiteSpace(f.Value))
            .ToList() ?? [];
        if (activeFilters.Count == 0 && string.IsNullOrWhiteSpace(sort))
        {
            return query;
        }

        var target = ProjectionTarget.Find(query.Expression, typeof(T));

        var source = target.Source;
        foreach (var (field, value) in activeFilters)
        {
            if (target.Resolve(field) is { } member && BuildPredicate(member, value.Trim()) is { } predicate)
            {
                source = Expression.Call(
                    typeof(Queryable), nameof(Queryable.Where), [target.SourceElementType], source,
                    Expression.Quote(Expression.Lambda(predicate, target.Parameter)));
            }
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var descending = sort.StartsWith('-');
            var field = sort.TrimStart('-', '+').Trim();
            if (target.Resolve(field) is { } member && IsSortable(member.Type))
            {
                source = Expression.Call(
                    typeof(Queryable), descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy),
                    [target.SourceElementType, member.Type], source,
                    Expression.Quote(Expression.Lambda(member, target.Parameter)));
            }
        }

        return query.Provider.CreateQuery<T>(target.Rebuild(source));
    }

    private static bool IsSortable(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) || underlying == typeof(decimal)
            || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan) || underlying == typeof(Guid);
    }

    private static Expression? BuildPredicate(Expression member, string value)
    {
        var type = member.Type;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            if (value.StartsWith('='))
            {
                return Expression.Equal(member, Expression.Constant(value[1..].Trim(), typeof(string)));
            }

            // Boş değerli kayıtlar elenir; LIKE, veritabanı karşılaştırma kuralıyla büyük/küçük harf duyarsızdır.
            return Expression.AndAlso(
                Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                Expression.Call(member, typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                    Expression.Constant(value)));
        }

        var parsed = Parse(underlying, value.TrimStart('='));
        return parsed is null ? null : Expression.Equal(member, Expression.Constant(parsed, type));
    }

    private static object? Parse(Type type, string value)
    {
        if (type.IsEnum)
        {
            // Enum değerleri API'de BÜYÜK_HARF_ALT_ÇİZGİ biçimindedir (örn. IN_PRODUCTION → InProduction).
            return Enum.TryParse(type, value.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true, out var result)
                ? result
                : null;
        }

        var invariant = CultureInfo.InvariantCulture;
        var number = value.Replace(',', '.');
        return type switch
        {
            _ when type == typeof(int) => int.TryParse(number, NumberStyles.Integer, invariant, out var v) ? v : null,
            _ when type == typeof(long) => long.TryParse(number, NumberStyles.Integer, invariant, out var v) ? v : null,
            _ when type == typeof(short) => short.TryParse(number, NumberStyles.Integer, invariant, out var v) ? v : null,
            _ when type == typeof(decimal) => decimal.TryParse(number, NumberStyles.Number, invariant, out var v) ? v : null,
            _ when type == typeof(double) => double.TryParse(number, NumberStyles.Float, invariant, out var v) ? v : null,
            _ when type == typeof(bool) => bool.TryParse(value, out var v) ? v : null,
            _ when type == typeof(DateOnly) => DateOnly.TryParseExact(value, "yyyy-MM-dd", invariant, DateTimeStyles.None, out var v) ? v : null,
            _ => null,
        };
    }

    /// <summary>Sorgunun son projeksiyonu: kaynağı, satır parametresi ve DTO alanlarının kaynak ifadeleri.</summary>
    private sealed class ProjectionTarget
    {
        private readonly Dictionary<string, Expression> members;
        private readonly Func<Expression, Expression> rebuild;

        private ProjectionTarget(
            Expression source, ParameterExpression parameter, Dictionary<string, Expression> members,
            Func<Expression, Expression> rebuild)
        {
            Source = source;
            Parameter = parameter;
            this.members = members;
            this.rebuild = rebuild;
        }

        public Expression Source { get; }
        public ParameterExpression Parameter { get; }
        public Type SourceElementType => Parameter.Type;

        public Expression? Resolve(string field) => members.GetValueOrDefault(field);

        public Expression Rebuild(Expression source) => rebuild(source);

        public static ProjectionTarget Find(Expression expression, Type elementType)
        {
            if (expression is MethodCallExpression { Method.Name: nameof(Queryable.Select) } select
                && select.Method.DeclaringType == typeof(Queryable)
                && select.Arguments[1] is UnaryExpression { Operand: LambdaExpression { Parameters.Count: 1 } projection })
            {
                var map = MembersOf(projection.Body);
                if (map.Count > 0)
                {
                    return new ProjectionTarget(
                        select.Arguments[0], projection.Parameters[0], map,
                        source => Expression.Call(select.Method, source, select.Arguments[1]));
                }
            }

            // Projeksiyon yoksa satırın kendi alanları kullanılır (sorgu doğrudan entity döndürüyorsa).
            var parameter = Expression.Parameter(elementType, "row");
            var own = elementType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => (Expression)Expression.Property(parameter, p), StringComparer.OrdinalIgnoreCase);
            return new ProjectionTarget(expression, parameter, own, source => source);
        }

        private static Dictionary<string, Expression> MembersOf(Expression body)
        {
            var map = new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
            switch (body)
            {
                // Konumsal record: kurucu parametre adı, özelliğin adıdır.
                case NewExpression { Constructor: not null } created:
                    var parameters = created.Constructor.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        map[parameters[i].Name!] = created.Arguments[i];
                    }
                    break;
                case MemberInitExpression init:
                    foreach (var binding in init.Bindings.OfType<MemberAssignment>())
                    {
                        map[binding.Member.Name] = binding.Expression;
                    }
                    break;
            }

            return map;
        }
    }
}
