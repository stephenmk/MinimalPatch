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

using System.Collections.ObjectModel;

namespace MinimalPatch.Internal;

internal sealed class Hunk
{
    public HunkHeader Header { get; }
    public ReadOnlyDictionary<int, List<LineOperation>> LineOperations { get; }

    public Hunk(ReadOnlySpan<char> header)
    {
        Header = new HunkHeader(header);

        // `.AsReadOnly()` because no additional keys should be added.
        LineOperations = Enumerable
            .Range(Header.StartA, Header.LengthA)
            .Select(static x => new KeyValuePair<int, List<LineOperation>>(x, []))
            .ToDictionary()
            .AsReadOnly();
    }

    public bool LengthsAreConsistent()
    {
        int aCount = 0;
        int bCount = 0;
        foreach (var opList in LineOperations.Values)
        {
            foreach (var op in opList)
            {
                if (op.IsOriginalLine()) aCount++;
                if (op.IsOutputLine()) bCount++;
            }
        }
        return Header.LengthA == aCount && Header.LengthB == bCount;
    }
}
