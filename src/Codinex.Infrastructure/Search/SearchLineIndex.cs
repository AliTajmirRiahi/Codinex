using System.Collections.Generic;

namespace Codinex.Infrastructure.Search;

/// <summary>
/// Converts character offsets into 1-based (line, column) pairs, matching the convention used
/// elsewhere in workspace-change resolution (only '\n' delimits lines; column counts characters since
/// the last '\n', so a trailing '\r' before it is counted as part of the previous line's column).
/// Newline offsets are indexed once per search so every match's line/column is resolved in O(log n)
/// instead of re-scanning the text from the start for each match.
/// </summary>
internal sealed class SearchLineIndex
{
    private readonly int[] _newlineOffsets;

    public SearchLineIndex(string text)
    {
        var offsets = new List<int>();

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                offsets.Add(i);
        }

        _newlineOffsets = offsets.ToArray();
    }

    public (int Line, int Column) GetLineColumn(int offset)
    {
        var newlinesBefore = CountNewlinesBefore(offset);
        var line = newlinesBefore + 1;
        var lastNewlineIndex = newlinesBefore > 0 ? _newlineOffsets[newlinesBefore - 1] : -1;
        var column = offset - lastNewlineIndex;

        return (line, column);
    }

    private int CountNewlinesBefore(int offset)
    {
        var lo = 0;
        var hi = _newlineOffsets.Length;

        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;

            if (_newlineOffsets[mid] < offset)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo;
    }
}
