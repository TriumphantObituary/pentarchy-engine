using System;
using System.Text;
using Pentarchy.Engine.Core.Primitives;

namespace Pentarchy.Engine.Diagnostics.Primitives;

/// <summary>
/// Audits relational GraphEdges, unraveling source/destination metrics and evaluating engine bitfield markers.
/// </summary>
public static class GraphEdgeVisualizer
{
    public static string Render(GraphEdge edge)
    {
        StringBuilder sb = new();
        sb.AppendLine($"[GraphEdge] Node({edge.SourceNodeId}) ──[{edge.TraversalCost:F3}]──> Node({edge.DestinationNodeId})");
        sb.AppendLine($"  Active Flags Bitfield: {Convert.ToString(edge.Flags, 2).PadLeft(32, '0')} (Raw: 0x{edge.Flags:X8})");
        return sb.ToString();
    }
}