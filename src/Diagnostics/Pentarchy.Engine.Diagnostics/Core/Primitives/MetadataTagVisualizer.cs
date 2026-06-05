using System;
using System.Text;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Diagnostics.Primitives;

/// <summary>
/// Provides debugging diagnostics for parsing textual MetadataTags and scanning null-terminator padding profiles.
/// </summary>
public static class MetadataTagVisualizer
{
    public static string Render(MetadataTag tag)
    {
        StringBuilder sb = new();
        sb.AppendLine($"[MetadataTag] \"{tag.Value}\"");
        
        // Project serialization footprint to visualize backing padding density
        Span<byte> scratch = stackalloc byte[MetadataTag.MaxTagSize];
        tag.Serialize(scratch);

        sb.Append("  Layout Footprint: [");
        for (int i = 0; i < scratch.Length; i++)
        {
            sb.Append(scratch[i] == 0x00 ? "\\0" : ((char)scratch[i]).ToString());
        }
        sb.AppendLine("]");
        return sb.ToString();
    }
}