namespace ProjectAW.Infrastructure.Errors;

/// <summary>
/// Represents a valid request that conflicts with the current business state,
/// such as attempting to create a duplicate code.
/// </summary>
public sealed class ConflictoNegocioException : Exception
{
    public ConflictoNegocioException(string message) : base(message) { }
}
