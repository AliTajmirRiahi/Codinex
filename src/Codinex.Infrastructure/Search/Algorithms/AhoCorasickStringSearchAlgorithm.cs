using System;
using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Aho-Corasick multi-pattern search: builds a trie of all patterns, augmented with failure links
/// (the same role KMP's failure function plays, generalized to a set of patterns) so the whole text is
/// scanned once regardless of how many patterns are searched for.
/// </summary>
/// <remarks>
/// Complexity: preprocessing O(sum of pattern lengths), search O(n + total number of matches),
/// independent of pattern count.
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class AhoCorasickStringSearchAlgorithm : IMultiPatternStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.AhoCorasick;

    public string Name => "Aho-Corasick";

    public IReadOnlyList<SearchMatch> Search(string text, string pattern, StringSearchOptions options)
    {
        SearchOptionsHelper.ValidateTextAndPattern(text, pattern);

        return SearchMultiple(text, new[] { pattern }, options);
    }

    public IReadOnlyList<SearchMatch> SearchMultiple(string text, IReadOnlyList<string> patterns, StringSearchOptions options)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        if (patterns == null)
            throw new ArgumentNullException(nameof(patterns));

        if (patterns.Count == 0)
            throw new ArgumentException("At least one pattern is required.", nameof(patterns));

        foreach (var p in patterns)
        {
            if (string.IsNullOrEmpty(p))
                throw new ArgumentException("Patterns must not be null or empty.", nameof(patterns));
        }

        SearchOptionsHelper.ValidateOptions(options);

        var ignoreCase = SearchOptionsHelper.IsCaseInsensitive(options);
        var haystack = SearchOptionsHelper.Normalize(text, ignoreCase);
        var normalizedPatterns = patterns
            .Select(p => SearchOptionsHelper.Normalize(p, ignoreCase))
            .ToArray();

        var wholeWord = SearchOptionsHelper.IsWholeWord(options);

        var trie = new Trie(normalizedPatterns);

        var matches = new List<SearchMatch>();
        var state = 0;

        for (var i = 0; i < haystack.Length; i++)
        {
            state = trie.Step(state, haystack[i]);

            foreach (var patternIndex in trie.Outputs(state))
            {
                var length = patterns[patternIndex].Length;
                var start = i - length + 1;

                if (wholeWord && !SearchOptionsHelper.IsWholeWordMatch(text, start, length))
                    continue;

                var match = SearchMatchFactory.CreateExact(text, start, length, Algorithm, options);
                match.MatchedPattern = patterns[patternIndex];
                match.PatternIndex = patternIndex;

                matches.Add(match);
            }
        }

        // Aho-Corasick emits matches in order of their *end* position; different pattern lengths mean
        // that isn't necessarily ascending start order, so restore the contract every algorithm honors.
        matches.Sort((a, b) => a.StartIndex != b.StartIndex
            ? a.StartIndex.CompareTo(b.StartIndex)
            : a.EndIndex.CompareTo(b.EndIndex));

        if (matches.Count > options.MaxResults)
            matches.RemoveRange(options.MaxResults, matches.Count - options.MaxResults);

        return matches;
    }

    /// <summary>The Aho-Corasick automaton: a trie plus failure links plus merged output sets.</summary>
    private sealed class Trie
    {
        private readonly List<Dictionary<char, int>> _children = new() { new Dictionary<char, int>() };
        private readonly List<int> _fail = new() { 0 };
        private readonly List<List<int>> _outputs = new() { new List<int>() };

        public Trie(IReadOnlyList<char[]> patterns)
        {
            for (var p = 0; p < patterns.Count; p++)
                Insert(patterns[p], p);

            BuildFailureLinks();
        }

        private void Insert(char[] pattern, int patternIndex)
        {
            var node = 0;

            foreach (var c in pattern)
            {
                if (!_children[node].TryGetValue(c, out var next))
                {
                    next = _children.Count;
                    _children.Add(new Dictionary<char, int>());
                    _fail.Add(0);
                    _outputs.Add(new List<int>());
                    _children[node][c] = next;
                }

                node = next;
            }

            _outputs[node].Add(patternIndex);
        }

        private void BuildFailureLinks()
        {
            var queue = new Queue<int>();

            foreach (var entry in _children[0])
            {
                var child = entry.Value;

                _fail[child] = 0;
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();

                foreach (var entry in _children[u])
                {
                    var c = entry.Key;
                    var v = entry.Value;

                    queue.Enqueue(v);

                    var f = _fail[u];

                    while (f != 0 && !_children[f].ContainsKey(c))
                        f = _fail[f];

                    _fail[v] = _children[f].TryGetValue(c, out var candidate) && candidate != v ? candidate : 0;

                    _outputs[v].AddRange(_outputs[_fail[v]]);
                }
            }
        }

        public int Step(int state, char c)
        {
            while (state != 0 && !_children[state].ContainsKey(c))
                state = _fail[state];

            return _children[state].TryGetValue(c, out var next) ? next : 0;
        }

        public IReadOnlyList<int> Outputs(int state) => _outputs[state];
    }
}
