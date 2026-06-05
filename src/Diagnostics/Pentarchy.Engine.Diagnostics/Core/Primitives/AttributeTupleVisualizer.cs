using System;
using System.Text;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Diagnostics.Primitives;

/// <summary>
/// Provides visual debugging lenses and IEEE-754 bitwise extraction maps for AttributeTuple primitives.
/// </summary>
public static class AttributeTupleVisualizer
{
    public static string Render(AttributeTuple attribute)
    {
        StringBuilder sb = new();
        sb.AppendLine($"[AttributeTuple] '{attribute.Key}'");
        sb.AppendLine($"  Value: {attribute.Value}");
        sb.Append(InspectIEEE754Double(attribute.Value));
        return sb.ToString();
    }

    /// <summary>
    /// Deconstructs the underlying double-precision float to diagnose precision truncation problems.
    /// </summary>
    public static string InspectIEEE754Double(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        long signBit = (bits >> 63) & 0x1;
        long exponentBits = (bits >> 52) & 0x7FF;
        long mantissaBits = bits & 0xFFFFFFFFFFFFF;

        StringBuilder sb = new();
        sb.AppendLine($"  ├─ Raw Hex: 0x{bits:X16}");
        sb.AppendLine($"  ├─ Sign:    {signBit} ({(signBit == 0 ? "Positive" : "Negative")})");
        sb.AppendLine($"  ├─ Bias-2 Exponent: {Convert.ToString(exponentBits, 2).PadLeft(11, '0')} (Scale: {exponentBits - 1023})");
        sb.AppendLine($"  └─ Mantissa Bits:   {Convert.ToString(mantissaBits, 2).PadLeft(52, '0')}");
        return sb.ToString();
    }
}