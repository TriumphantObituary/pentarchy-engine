using System;
using System.Text;
using Pentarchy.Engine.Core.Memory;

namespace Pentarchy.Engine.Diagnostics.Core;

/// <summary>
/// Provides advanced debugging lenses and visual diagnostic views looking 
/// directly into the raw binary memory state of a MemoryPage.
/// </summary>
public static class MemoryPageVisualizer
{
    /// <summary>
    /// Generates a comprehensive string mapping out a targeted window of a MemoryPage's binary layout.
    /// Allows surgical inspection of deep memory offsets without rendering empty preceding blocks.
    /// </summary>
    public static string RenderPageDump(MemoryPage page, int offsetStart = 0, int bytesToScan = 128)
    {
        // 1. Enforce strict safety guardrails on our memory constraints
        if (offsetStart < 0 || offsetStart >= MemoryPage.PageSize)
        {
            offsetStart = 0;
        }

        // Clamp the scan length to ensure our window cannot run off the cliff of physical memory
        if (offsetStart + bytesToScan > MemoryPage.PageSize)
        {
            bytesToScan = MemoryPage.PageSize - offsetStart;
        }

        // Align our visual offset start backward to the nearest 16-byte row boundary 
        // to maintain crisp terminal column layout alignment
        int alignedStart = (offsetStart / 16) * 16;
        int alignedLength = bytesToScan + (offsetStart - alignedStart);
        
        // Ensure our adjusted length doesn't overflow the page boundary either
        if (alignedStart + alignedLength > MemoryPage.PageSize)
        {
            alignedLength = MemoryPage.PageSize - alignedStart;
        }
        
        ReadOnlySpan<byte> memoryBuffer = page.Buffer;

        StringBuilder output = new StringBuilder();
        output.AppendLine("=============================================================================");
        output.AppendLine($"| PENTARCHY RAW CORE DIAGNOSTIC INTRUSION MAP                              |");
        output.AppendLine($"| WINDOW: BYTES {alignedStart} TO {alignedStart + alignedLength} / {MemoryPage.PageSize} TOTAL FIXED MEMORY                 |");
        output.AppendLine("=============================================================================");
        output.AppendLine("OFFSET    00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F  ASCII REPR");
        output.AppendLine("-----------------------------------------------------------------------------");

        // Process our memory buffer slice in standard 16-byte rows
        for (int rowOffset = alignedStart; rowOffset < alignedStart + alignedLength; rowOffset += 16)
        {
            // Append the hex address coordinate pointer relative to absolute page memory
            output.Append($"{rowOffset:X8}  ");

            int rowLength = Math.Min(16, (alignedStart + alignedLength) - rowOffset);

            // Render the Hexadecimal Column Matrix
            for (int i = 0; i < 16; i++)
            {
                if (i == 8) output.Append(" "); 

                if (i < rowLength)
                {
                    byte currentByte = memoryBuffer[rowOffset + i];
                    output.Append($"{currentByte:X2} ");
                }
                else
                {
                    output.Append("   "); 
                }
            }

            output.Append(" ");

            // Render the corresponding ASCII Character Column
            for (int i = 0; i < rowLength; i++)
            {
                byte currentByte = memoryBuffer[rowOffset + i];

                if (currentByte >= 32 && currentByte <= 126)
                {
                    output.Append((char)currentByte);
                }
                else
                {
                    output.Append('.'); 
                }
            }

            output.AppendLine();
        }

        output.AppendLine("=============================================================================");
        return output.ToString();
    }

    /// <summary>
    /// Renders a highly compacted structural map showing exactly which data slots
    /// are active vs unallocated inside a targeted window of a MemoryPage.
    /// </summary>
    public static string RenderAllocationGrid(MemoryPage page, int startSlot = 0, int slotsToScan = 8)
    {
        // Enforce boundary safety limits for slots
        // Total slots available on a 1KB page with a 16-byte header: (1024 - 16) / 32 = 31 slots max
        const int maxSlots = 31; 

        if (startSlot < 0 || startSlot > maxSlots) startSlot = 0;
        if (startSlot + slotsToScan > maxSlots + 1) slotsToScan = (maxSlots + 1) - startSlot;

        StringBuilder output = new StringBuilder();
        output.AppendLine($"=============================================================================");
        output.AppendLine($"| PAGE ALLOCATION STRUCTURAL MATRIX (SLOTS {startSlot:D2} TO {startSlot + slotsToScan - 1:D2})                    |");
        output.AppendLine($"=============================================================================");
        
        ReadOnlySpan<byte> buffer = page.Buffer;

        for (int i = 0; i < slotsToScan; i++)
        {
            int currentSlot = startSlot + i;
            int startByte = 16 + (currentSlot * 32);

            // Peek at the very first byte of the text key to check allocation status
            bool isAllocated = buffer[startByte] != 0x00;

            if (isAllocated)
            {
                // 1. Rehydrate the 24-byte Text Key
                byte[] tempKeyBytes = new byte[24];
                for (int j = 0; j < 24; j++) tempKeyBytes[j] = buffer[startByte + j];
                string keyName = Encoding.UTF8.GetString(tempKeyBytes).TrimEnd('\0');

                // 2. Rehydrate the 8-byte IEEE Double Value
                ReadOnlySpan<byte> valueSlice = buffer.Slice(startByte + 24, 8);
                double rawValue = BitConverter.ToDouble(valueSlice);

                output.AppendLine($"  [X] Slot {currentSlot:D2}: '{keyName,-14}' => VALUE: {rawValue,-10} | (Size: 32 Bytes)");
            }
            else
            {
                string freeLabel = "[UNALLOCATED]";
                output.AppendLine($"  [ ] Slot {currentSlot:D2}: '{freeLabel,-14}' => VALUE: 0.0        | (Status: FREE)");
            }
        }
        
        output.AppendLine("=============================================================================");
        return output.ToString();
    }

    /// <summary>
    /// Advanced Bit-Level Deconstructor. Inspects the exact IEEE 754 floating point binary structure
    /// of an 8-byte segment inside a slot buffer to expose mathematical truncation states.
    /// </summary>
    public static string InspectIEEE754Double(double value)
    {
        // Convert our 64-bit double into its raw 64-bit integer representation
        long bits = BitConverter.DoubleToInt64Bits(value);
        
        // Extract components using bitwise shifting and masking operators
        long signBit = (bits >> 63) & 0x1;
        long exponentBits = (bits >> 52) & 0x7FF;
        long mantissaBits = bits & 0xFFFFFFFFFFFFF; // 52-bit mask

        StringBuilder output = new StringBuilder();
        output.AppendLine($"--- IEEE-754 BINARY DECONSTRUCTION: ({value}) ---");
        output.AppendLine($"  Raw Hex Value: 0x{bits:X16}");
        output.AppendLine($"  Sign Bit:      {signBit}        -> {(signBit == 0 ? "Positive (+)" : "Negative (-)")}");
        output.AppendLine($"  Exponent Bits: {Convert.ToString(exponentBits, 2).PadLeft(11, '0')} -> (Base-2 Scale: {exponentBits - 1023})");
        output.AppendLine($"  Mantissa Bits: {Convert.ToString(mantissaBits, 2).PadLeft(52, '0')}");
        output.AppendLine($"---------------------------------------------------------");
        
        return output.ToString();
    }
}