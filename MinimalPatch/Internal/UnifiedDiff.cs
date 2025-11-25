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

namespace MinimalPatch.Internal;

internal sealed class UnifiedDiff
{
    private readonly List<Hunk> _hunks = [];
    private readonly int _sumLengthA = 0;
    private ref struct ConstructorState
    {
        public int CurrentDiffLineNum;
        public int CurrentLineNumA;
        public int CurrentLengthA;
        public int CurrentLengthB;
    }

    public UnifiedDiff(ReadOnlySpan<char> text)
    {
        ConstructorState state = new();
        Hunk? hunk = null;

        foreach (var range in text.Split('\n'))
        {
            state.CurrentDiffLineNum++;
            var line = text[range];

            if (state.CurrentDiffLineNum < 3)
            {
                ValidateHeaderLine(line, state.CurrentDiffLineNum);
            }
            else if (line.StartsWith('@'))
            {
                AddHunk(hunk, ref state);
                hunk = new Hunk(line);
                _sumLengthA += hunk.Header.LengthA;
                state.CurrentLineNumA = hunk.Header.StartA - 1;
            }
            else if (line.Length > 0 && GetLineOperation(line[0]) is Operation operation)
            {
                AddLineOperation(hunk, operation, range, ref state);
            }
            else if (range.Start.Equals(text.Length) && range.End.Equals(text.Length))
            {
                // Blank line at the end of the file.
            }
            else
            {
                throw new InvalidDiffException($"Line #{state.CurrentDiffLineNum} in unidiff text does not begin with a standard prefix");
            }
        }

        AddHunk(hunk, ref state);
    }

    private static void ValidateHeaderLine(ReadOnlySpan<char> line, int diffLineNum)
    {
        if (!line.StartsWith(HeaderPrefix(diffLineNum), StringComparison.Ordinal))
        {
            throw new InvalidDiffException("UnifiedDiff text does not begin with the standard header");
        }
    }

    private static ReadOnlySpan<char> HeaderPrefix(int lineNumber) => lineNumber switch
    {
        1 => ['-', '-', '-'],
        2 => ['+', '+', '+'],
        _ => throw new ArgumentOutOfRangeException(nameof(lineNumber))
    };

    private void AddHunk(Hunk? hunk, ref ConstructorState state)
    {
        if (hunk is null)
        {
            return;
        }
        if (state.CurrentLengthA == hunk.Header.LengthA && state.CurrentLengthB == hunk.Header.LengthB)
        {
            _hunks.Add(hunk);
            state.CurrentLengthA = 0;
            state.CurrentLengthB = 0;
        }
        else
        {
            throw new InvalidDiffException("Hunk header does not match count of line operations");
        }
    }

    private static Operation? GetLineOperation(char prefix) => prefix switch
    {
        ' ' => Operation.Equal,
        '-' => Operation.Delete,
        '+' => Operation.Insert,
        '\\' => throw new NotSupportedException("'No newline at end of file' operations have not been implemented"),
        _ => null
    };

    private static void AddLineOperation(Hunk? hunk, Operation operation, Range range, ref ConstructorState state)
    {
        if (hunk is null)
        {
            throw new InvalidDiffException("Line operation found before any hunks");
        }
        if (operation.IsFileA())
        {
            state.CurrentLineNumA++;
            state.CurrentLengthA++;
        }
        if (operation.IsFileB())
        {
            state.CurrentLengthB++;
        }

        // CurrentLineNumA is initialized to StartA - 1.
        // If the first operations are inserts, then CurrentLineNumA
        // will be less than StartA and therefore out of range.
        int idx = int.Max(state.CurrentLineNumA, hunk.Header.StartA) - hunk.Header.StartA;

        hunk.LineOperations[idx].Add(new LineOperation
        {
            Operation = operation,
            Range = new Range(range.Start.Value + 1, range.End),
        });
    }

    public FrozenDictionary<int, List<LineOperation>> GetLineOperations()
    {
        var pairs = new KeyValuePair<int, List<LineOperation>>[_sumLengthA];
        int pairIdx = 0;
        foreach (var hunk in _hunks)
        {
            for (int i = 0; i < hunk.Header.LengthA; i++)
            {
                int lineNumber = hunk.Header.StartA + i;
                pairs[pairIdx] = new(lineNumber, hunk.LineOperations[i]);
                pairIdx++;
            }
        }
        return pairs.ToFrozenDictionary();
    }
}
