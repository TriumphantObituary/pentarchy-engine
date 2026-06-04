using System;
using System.Text;
using Pentarchy.Engine.Core.Memory;

namespace Pentarchy.Engine.Diagnostics;

/// <summary>
/// A diagnostic inspection utility that renders raw memory pages into structured, 
/// industry-standard Hexadecimal and ASCII cryptographic terminal readouts.
/// </summary>
public static class HexDumpVisualizer
{
    /// <summary>
    /// Generates a comprehensive string mapping out the entire binary layout of a MemoryPage.
    /// </summary>
    public static string RenderPageDump(MemoryPage page, int bytesToScan = 128)
    {
        // Clamp our scan boundary to ensure we don't violate physical page sizing
        bytesToScan = Math.Min(bytesToScan, MemoryPage.PageSize);
        
        // Grab a direct, zero-allocation window looking into the page's raw memory
        ReadOnlySpan<byte> memoryBuffer = page.Buffer;

        StringBuilder output = new StringBuilder();
        output.AppendLine("=============================================================================");
        output.AppendLine($"| PENTARCHY RAW CORE DIAGNOSTIC INTRUSION MAP                              |");
        output.AppendLine($"| SCAN BOUNDARY: {bytesToScan} / {MemoryPage.PageSize} BYTES FIXED SYSTEM MEMORY                      |");
        output.AppendLine("=============================================================================");
        output.AppendLine("OFFSET    00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F  ASCII REPR");
        output.AppendLine("-----------------------------------------------------------------------------");

        // Process our memory buffer in standard 16-byte technical rows
        for (int rowOffset = 0; rowOffset < bytesToScan; rowOffset += 16)
        {
            // 1. Append the hex address coordinate pointer (e.g. 0x00000010)
            output.Append($"{rowOffset:X8}  ");

            // Calculate the size of our current row chunk (handles non-16 partial trailing blocks safely)
            int rowLength = Math.Min(16, bytesToScan - rowOffset);

            // 2. Render the Hexadecimal Column Matrix
            for (int i = 0; i < 16; i++)
            {
                if (i == 8) 
                {
                    output.Append(" "); // Insert an extra spacer to separate bytes 0-7 from 8-15 visually
                }

                if (i < rowLength)
                {
                    byte currentByte = memoryBuffer[rowOffset + i];
                    output.Append($"{currentByte:X2} ");
                }
                else
                {
                    output.Append("   "); // Fill empty space if the scan line terminates short
                }
            }

            output.Append(" ");

            // 3. Render the corresponding ASCII Character Column
            for (int i = 0; i < rowLength; i++)
            {
                byte currentByte = memoryBuffer[rowOffset + i];

                // Is this byte a printable human character? (Standard ASCII printable bounds)
                if (currentByte >= 32 && currentByte <= 126)
                {
                    output.Append((char)currentByte);
                }
                else
                {
                    output.Append('.'); // Substitute control, numeric, or null bytes with a clean dot
                }
            }

            output.AppendLine();
        }

        output.AppendLine("=============================================================================");
        return output.ToString();
    }
}