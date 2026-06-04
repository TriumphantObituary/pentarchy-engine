namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// The quantitative atom of the Pentarchy Engine. 
/// Represents any flat metric, resource, or physical property.
/// </summary>
public class AttributeTuple
{
    public string Key { get; }
    public double Value { get; }

    public AttributeTuple(string key, double value)
    {
        // Add defensive layout guardrail
        if (key != null && key.Length > 24)
        {
            throw new ArgumentException("Key name length cannot exceed 24 characters to respect slot bounds.", nameof(key));
        }

        Key = key ?? string.Empty;
        Value = value;
    }
}