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

namespace MinimalPatch.Internal;

internal readonly record struct HunkHeader
{
    public readonly int StartA { get; }
    public readonly int LengthA { get; }
    public readonly int LengthB { get; }

    public HunkHeader(ReadOnlySpan<char> text)
    {
        bool seenHeaderStart = false;
        foreach (var range in text.Split(' '))
        {
            var subtext = text[range];
            if (subtext.StartsWith('-'))
            {
                (StartA, LengthA) = GetStartAndLength(subtext[1..]);
            }
            else if (subtext.StartsWith('+'))
            {
                // StartB isn't used for anything.
                (_, LengthB) = GetStartAndLength(subtext[1..]);
            }
            else if (subtext.StartsWith('@') && !seenHeaderStart)
            {
                seenHeaderStart = true;
            }
            else
            {
                break;
            }
        }
    }

    private static (int, int) GetStartAndLength(ReadOnlySpan<char> text)
    {
        int i = text.IndexOf(',');
        if (i == -1)
        {
            return (int.Parse(text), 1);
        }
        else
        {
            int start = int.Parse(text[..i]);
            int length = int.Parse(text[(i + 1)..]);
            return (start, length);
        }
    }
}
