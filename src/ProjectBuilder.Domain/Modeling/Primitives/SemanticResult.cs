namespace ProjectBuilder.Domain.Modeling.Primitives;

/// <summary>
/// Represents the explicit outcome of validating or applying a semantic operation.
/// </summary>
/// <typeparam name="T">The accepted value type.</typeparam>
public abstract record SemanticResult<T>
    where T : notnull
{
    private SemanticResult()
    {
    }

    public sealed record Accepted(T Value) : SemanticResult<T>;

    public sealed record Rejected(SemanticError Error) : SemanticResult<T>;

}

public static class SemanticResult
{
    public static SemanticResult<T> Accept<T>(T value)
        where T : notnull =>
        new SemanticResult<T>.Accepted(value);

    public static SemanticResult<T> Reject<T>(string code, string message)
        where T : notnull =>
        new SemanticResult<T>.Rejected(new SemanticError(code, message));
}

/// <summary>
/// Describes a stable, machine-readable semantic rejection.
/// </summary>
public sealed record SemanticError
{
    public SemanticError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}
