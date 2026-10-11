using System.Collections.Concurrent;

namespace YazimAsistani.Core;

public sealed class AdaptiveSuggestionEngine : ISuggestionEngine
{
    private readonly ConcurrentDictionary<EntryKey, EntryState> _entries = new();
    private readonly ConcurrentDictionary<BigramKey, int> _bigrams = new();

    public AdaptiveSuggestionEngine(IEnumerable<LexiconEntry> seedEntries)
    {
        ArgumentNullException.ThrowIfNull(seedEntries);

        foreach (var entry in seedEntries)
        {
            AddOrUpdate(entry);
        }
    }

    public IReadOnlyList<Suggestion> Suggest(
        string input,
        InputLanguage language,
        string? previousWord = null,
        int limit = 6)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<Suggestion>();
        }

        var query = TextNormalizer.NormalizeForLookup(input, language);
        var previous = string.IsNullOrWhiteSpace(previousWord)
            ? null
            : TextNormalizer.NormalizeForLookup(previousWord, language);
        var maximumDistance = query.Length <= 4 ? 1 : 2;

        return _entries
            .Where(pair => pair.Key.Language == language)
            .Select(pair => ScoreCandidate(pair.Key, pair.Value, query, previous, maximumDistance))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Text.Length)
            .ThenBy(candidate => candidate.Text, StringComparer.CurrentCulture)
            .Take(limit)
            .ToArray();
    }

    public IReadOnlyList<Suggestion> PredictNext(
        string previousWord,
        InputLanguage language,
        int limit = 6)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(previousWord))
        {
            return Array.Empty<Suggestion>();
        }

        var previous = TextNormalizer.NormalizeForLookup(previousWord, language);

        return _bigrams
            .Where(pair => pair.Key.Language == language && pair.Key.Previous == previous)
            .Select(pair =>
            {
                var entryKey = new EntryKey(language, pair.Key.Next);
                return _entries.TryGetValue(entryKey, out var entry)
                    ? new Suggestion(
                        entry.DisplayText,
                        SuggestionKind.NextWord,
                        (pair.Value * 20) + FrequencyScore(entry.Frequency) + (entry.SelectionCount * 5))
                    : null;
            })
            .Where(suggestion => suggestion is not null)
            .Select(suggestion => suggestion!)
            .OrderByDescending(suggestion => suggestion.Score)
            .Take(limit)
            .ToArray();
    }

    public void LearnText(string text, InputLanguage language)
    {
        var words = TextNormalizer.Tokenize(text);
        string? previous = null;

        foreach (var word in words)
        {
            var normalized = TextNormalizer.NormalizeForLookup(word, language);
            if (normalized.Length == 0)
            {
                continue;
            }

            var key = new EntryKey(language, normalized);
            _entries.AddOrUpdate(
                key,
                _ => new EntryState(word, 1, new HashSet<string>(StringComparer.Ordinal) { normalized }),
                (_, existing) => existing with { Frequency = existing.Frequency + 1 });

            if (previous is not null)
            {
                _bigrams.AddOrUpdate(
                    new BigramKey(language, previous, normalized),
                    1,
                    (_, count) => count + 1);
            }

            previous = normalized;
        }
    }

    public void RememberSelection(string word, InputLanguage language)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        var key = new EntryKey(language, TextNormalizer.NormalizeForLookup(word, language));
        _entries.AddOrUpdate(
            key,
            _ => new EntryState(word, 1, new HashSet<string>(StringComparer.Ordinal) { key.Normalized }),
            (_, existing) => existing with { SelectionCount = existing.SelectionCount + 1 });
    }

    public void AddOrUpdateWord(LexiconEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        AddOrUpdate(entry);
    }

    public SuggestionEngineState ExportState()
    {
        var words = _entries
            .Select(pair => new StoredWord(
                pair.Value.DisplayText,
                pair.Key.Language,
                pair.Value.Frequency,
                pair.Value.SelectionCount,
                pair.Value.Aliases.Order(StringComparer.Ordinal).ToArray()))
            .OrderBy(word => word.Language)
            .ThenBy(word => word.Text, StringComparer.Ordinal)
            .ToArray();

        var bigrams = _bigrams
            .Select(pair => new StoredBigram(
                pair.Key.Language,
                pair.Key.Previous,
                pair.Key.Next,
                pair.Value))
            .OrderBy(bigram => bigram.Language)
            .ThenBy(bigram => bigram.Previous, StringComparer.Ordinal)
            .ThenBy(bigram => bigram.Next, StringComparer.Ordinal)
            .ToArray();

        return new SuggestionEngineState(words, bigrams);
    }

    public void ImportState(SuggestionEngineState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        foreach (var word in state.Words.Where(word => !string.IsNullOrWhiteSpace(word.Text)))
        {
            var normalized = TextNormalizer.NormalizeForLookup(word.Text, word.Language);
            var key = new EntryKey(word.Language, normalized);
            var aliases = new HashSet<string>(
                word.Aliases
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Select(alias => TextNormalizer.NormalizeForLookup(alias, word.Language))
                    .Append(normalized),
                StringComparer.Ordinal);

            _entries.AddOrUpdate(
                key,
                _ => new EntryState(
                    word.Text,
                    Math.Max(1, word.Frequency),
                    aliases,
                    Math.Max(0, word.SelectionCount)),
                (_, existing) => existing with
                {
                    Frequency = Math.Max(existing.Frequency, word.Frequency),
                    SelectionCount = Math.Max(existing.SelectionCount, word.SelectionCount),
                    Aliases = new HashSet<string>(existing.Aliases.Concat(aliases), StringComparer.Ordinal)
                });
        }

        foreach (var bigram in state.Bigrams.Where(bigram => bigram.Count > 0))
        {
            var key = new BigramKey(
                bigram.Language,
                TextNormalizer.NormalizeForLookup(bigram.Previous, bigram.Language),
                TextNormalizer.NormalizeForLookup(bigram.Next, bigram.Language));
            _bigrams.AddOrUpdate(key, bigram.Count, (_, existing) => Math.Max(existing, bigram.Count));
        }
    }

    private void AddOrUpdate(LexiconEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Text))
        {
            return;
        }

        var normalized = TextNormalizer.NormalizeForLookup(entry.Text, entry.Language);
        var aliases = new HashSet<string>(StringComparer.Ordinal) { normalized };

        if (entry.Aliases is not null)
        {
            foreach (var alias in entry.Aliases.Where(alias => !string.IsNullOrWhiteSpace(alias)))
            {
                aliases.Add(TextNormalizer.NormalizeForLookup(alias, entry.Language));
            }
        }

        var key = new EntryKey(entry.Language, normalized);
        _entries.AddOrUpdate(
            key,
            _ => new EntryState(entry.Text, Math.Max(1, entry.Frequency), aliases),
            (_, existing) => existing with
            {
                Frequency = Math.Max(existing.Frequency, entry.Frequency),
                Aliases = new HashSet<string>(existing.Aliases.Concat(aliases), StringComparer.Ordinal)
            });
    }

    private Suggestion? ScoreCandidate(
        EntryKey key,
        EntryState state,
        string query,
        string? previous,
        int maximumDistance)
    {
        var isPrefix = state.Aliases.Any(alias => alias.StartsWith(query, StringComparison.Ordinal));
        var distance = isPrefix
            ? 0
            : state.Aliases.Min(alias => Levenshtein.Distance(query, alias, maximumDistance));

        if (!isPrefix && distance > maximumDistance)
        {
            return null;
        }

        double score = isPrefix
            ? 120 - Math.Min(45, Math.Abs(key.Normalized.Length - query.Length) * 2)
            : 72 - (distance * 18);

        score += FrequencyScore(state.Frequency);
        score += state.SelectionCount * 6;

        if (previous is not null &&
            _bigrams.TryGetValue(new BigramKey(key.Language, previous, key.Normalized), out var bigramCount))
        {
            score += Math.Min(50, bigramCount * 10);
        }

        return new Suggestion(
            state.DisplayText,
            isPrefix ? SuggestionKind.Completion : SuggestionKind.SpellingCorrection,
            score);
    }

    private static double FrequencyScore(int frequency) => Math.Log2(Math.Max(1, frequency) + 1) * 4;

    private readonly record struct EntryKey(InputLanguage Language, string Normalized);

    private readonly record struct BigramKey(InputLanguage Language, string Previous, string Next);

    private sealed record EntryState(
        string DisplayText,
        int Frequency,
        HashSet<string> Aliases,
        int SelectionCount = 0);
}

