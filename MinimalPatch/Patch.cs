/*
Copyright (c) 2025 Stephen Kraus

This file is part of MinimalPatch.

MinimalPatch is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MinimalPatch is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
General Public License for more details.

You should have received a copy of the GNU General Public License
along with MinimalPatch. If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections.Frozen;
using MinimalPatch.Internal;

namespace MinimalPatch;

/// <include file='docs.xml' path='docs/class[@name="Patch"]/*'/>
public static class Patch
{
    /// <include file='docs.xml' path='docs/method[@name="TryApply"]/*'/>
    public static bool TryApply(ReadOnlySpan<char> patch, ReadOnlySpan<char> original, Span<char> destination, out int charsWritten)
    {
        try
        {
            charsWritten = Apply(patch, original, destination);
            return true;
        }
        catch
        {
            charsWritten = default;
            return false;
        }
    }

    /// <include file='docs.xml' path='docs/method[@name="Apply" and @overload="0"]/*'/>
    public static string Apply(ReadOnlySpan<char> patch, ReadOnlySpan<char> original)
    {
        // The length of the resulting text is strictly less than the
        // combined length of the patch text and the original text.
        var destination = (new char[patch.Length + original.Length]).AsSpan();
        int charsWritten = Apply(patch, original, destination);
        return string.Create
        (
            length: charsWritten,
            state: destination[..charsWritten],
            action: static (output, state) => state.CopyTo(output)
        );
    }

    /// <include file='docs.xml' path='docs/method[@name="Apply" and @overload="1"]/*'/>
    public static int Apply(ReadOnlySpan<char> patch, ReadOnlySpan<char> original, Span<char> destination)
    {
        Range currentRange = default;
        int lineNumber = 0;
        int charsWritten = 0;

        var lineOperations = GetLineOperations(patch);

        foreach (var range in original.Split('\n'))
        {
            lineNumber++;
            if (lineOperations.TryGetValue(lineNumber, out var operations))
            {
                if (!currentRange.Equals(default))
                {
                    charsWritten = destination.AppendLine(original[currentRange], start: charsWritten);
                    currentRange = default;
                }
                foreach (var operation in operations)
                {
                    var operationText = patch[operation.Range];
                    if (operation.IsOriginalLine())
                    {
                        Validate(expected: operationText, actual: original[range], lineNumber);
                    }
                    if (operation.IsOutputLine())
                    {
                        charsWritten = destination.AppendLine(operationText, start: charsWritten);
                    }
                }
            }
            else
            {
                currentRange = currentRange.Equals(default)
                    ? range
                    : new Range(currentRange.Start, range.End);
            }
        }

        if (!currentRange.Equals(default))
        {
            charsWritten = destination.AppendLine(original[currentRange], start: charsWritten);
        }

        return charsWritten;
    }

    private static int AppendLine(this Span<char> buffer, ReadOnlySpan<char> line, int start)
    {
        if (start > 0)
        {
            buffer[start] = '\n';
            start++;
        }
        line.CopyTo(buffer[start..]);
        return start + line.Length;
    }

    private static FrozenDictionary<int, List<LineOperation>> GetLineOperations(ReadOnlySpan<char> patch)
    {
        try
        {
            UnifiedDiff diff = new(patch);
            return diff.GetLineOperations();
        }
        catch (Exception ex)
        {
            throw new InvalidPatchException("Error occurred while parsing patch text", ex);
        }
    }

    private static void Validate(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual, int lineNumber)
    {
        if (!expected.Equals(actual, StringComparison.Ordinal))
        {
            throw new InvalidPatchException($"Line #{lineNumber} of original text does not match the corresponding line in the patch");
        }
    }
}
