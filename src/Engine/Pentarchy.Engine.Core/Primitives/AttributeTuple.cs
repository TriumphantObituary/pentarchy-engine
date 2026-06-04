namespace Pentarchy.Engine.Core.Primitives;

/// <summary>
/// The quantitative atom of the Pentarchy Engine. 
/// Represents any flat metric, resource, or physical property.
/// </summary>
public readonly record struct AttributeTuple(string Key, double Value);