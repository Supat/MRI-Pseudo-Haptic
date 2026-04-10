namespace KLabPseudoHaptic.Exceptions;

/// <summary>
/// Base exception for all KLabPseudoHaptic SDK errors.
/// </summary>
public class GenICamException : Exception
{
    /// <summary>Creates a new instance.</summary>
    public GenICamException() { }

    /// <summary>Creates a new instance with the given message.</summary>
    public GenICamException(string message) : base(message) { }

    /// <summary>Creates a new instance with the given message and inner exception.</summary>
    public GenICamException(string message, Exception innerException)
        : base(message, innerException) { }
}
