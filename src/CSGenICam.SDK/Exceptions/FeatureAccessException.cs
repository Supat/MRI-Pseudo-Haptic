namespace CSGenICam.SDK.Exceptions;

/// <summary>
/// Thrown when reading or writing a GenICam feature fails.
/// </summary>
public sealed class FeatureAccessException : GenICamException
{
    /// <summary>Creates a new instance.</summary>
    public FeatureAccessException(string featureName, string message)
        : base($"Feature '{featureName}': {message}")
    {
        FeatureName = featureName;
    }

    /// <summary>Name of the feature that caused the error.</summary>
    public string FeatureName { get; }
}
