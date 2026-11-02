namespace ERP.Core.Application.Common;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    BusinessRule,
}

/// <summary>
/// Beklenen iş hataları exception yerine Result ile döner (RFID okutma gibi yoğun akışlarda
/// reddedilen işlemler olağan durumdur). <see cref="Code"/> API'de business error code olur.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.BusinessRule,
    IReadOnlyDictionary<string, string[]>? Details = null);

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız sonucun değeri okunamaz.");

    public static Result<T> Success(T value) => new(value, null);
    public static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
