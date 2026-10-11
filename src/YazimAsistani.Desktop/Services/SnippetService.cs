using YazimAsistani.Core;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed class SnippetService
{
    private readonly object _syncRoot = new();
    private readonly SnippetStore _store;
    private readonly List<SnippetDefinition> _snippets;

    public SnippetService(SnippetStore store)
    {
        _store = store;
        _snippets = store.Load()
            .Where(IsValid)
            .OrderBy(snippet => snippet.Trigger, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<SnippetDefinition> GetAll()
    {
        lock (_syncRoot)
        {
            return _snippets.ToArray();
        }
    }

    public string? TryExpand(string trigger, InputLanguage language)
    {
        var normalized = TextNormalizer.NormalizeForLookup(trigger, language);
        lock (_syncRoot)
        {
            return _snippets.FirstOrDefault(snippet =>
                snippet.Language == language &&
                TextNormalizer.NormalizeForLookup(snippet.Trigger, language) == normalized)?.Expansion;
        }
    }

    public void AddOrUpdate(string trigger, string expansion, InputLanguage language)
    {
        trigger = trigger.Trim();
        expansion = expansion.Trim();

        if (trigger.Length is < 2 or > 40 || trigger.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Tetikleyici 2-40 karakter olmalı ve boşluk içermemelidir.", nameof(trigger));
        }

        if (expansion.Length is < 1 or > 4000)
        {
            throw new ArgumentException("Genişletilmiş metin 1-4000 karakter olmalıdır.", nameof(expansion));
        }

        var normalized = TextNormalizer.NormalizeForLookup(trigger, language);
        lock (_syncRoot)
        {
            var index = _snippets.FindIndex(snippet =>
                snippet.Language == language &&
                TextNormalizer.NormalizeForLookup(snippet.Trigger, language) == normalized);
            var definition = new SnippetDefinition(trigger, expansion, language);

            if (index >= 0)
            {
                _snippets[index] = definition with { UseCount = _snippets[index].UseCount };
            }
            else
            {
                _snippets.Add(definition);
            }

            SortAndSave();
        }
    }

    public void Remove(SnippetDefinition definition)
    {
        lock (_syncRoot)
        {
            _snippets.RemoveAll(snippet =>
                snippet.Language == definition.Language &&
                string.Equals(snippet.Trigger, definition.Trigger, StringComparison.CurrentCultureIgnoreCase));
            SortAndSave();
        }
    }

    public void RememberUse(string trigger, InputLanguage language)
    {
        var normalized = TextNormalizer.NormalizeForLookup(trigger, language);
        lock (_syncRoot)
        {
            var index = _snippets.FindIndex(snippet =>
                snippet.Language == language &&
                TextNormalizer.NormalizeForLookup(snippet.Trigger, language) == normalized);
            if (index < 0)
            {
                return;
            }

            _snippets[index] = _snippets[index] with { UseCount = _snippets[index].UseCount + 1 };
            SortAndSave();
        }
    }

    private static bool IsValid(SnippetDefinition snippet) =>
        !string.IsNullOrWhiteSpace(snippet.Trigger) &&
        !string.IsNullOrWhiteSpace(snippet.Expansion);

    private void SortAndSave()
    {
        _snippets.Sort((left, right) => string.Compare(
            left.Trigger,
            right.Trigger,
            StringComparison.CurrentCultureIgnoreCase));
        _store.Save(_snippets);
    }
}
