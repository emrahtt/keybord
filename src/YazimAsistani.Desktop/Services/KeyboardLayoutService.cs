using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed class KeyboardLayoutService
{
    private readonly object _syncRoot = new();
    private readonly KeyboardLayoutStore _store;
    private readonly List<KeyboardLayoutDefinition> _customLayouts;

    public KeyboardLayoutService(KeyboardLayoutStore store)
    {
        _store = store;
        _customLayouts = store.Load()
            .Where(layout => !layout.IsBuiltIn && IsValid(layout))
            .ToList();
    }

    public IReadOnlyList<KeyboardLayoutDefinition> GetAll()
    {
        lock (_syncRoot)
        {
            return BuiltInLayouts.Concat(_customLayouts).ToArray();
        }
    }

    public IReadOnlyList<KeyboardLayoutDefinition> GetForLanguage(InputLanguage language)
    {
        lock (_syncRoot)
        {
            return BuiltInLayouts
                .Concat(_customLayouts)
                .Where(layout => layout.Language is null || layout.Language == language)
                .ToArray();
        }
    }

    public void AddOrUpdate(string? id, string name, InputLanguage? language, IEnumerable<string> keys)
    {
        name = name.Trim();
        var normalizedKeys = keys
            .Select(key => key.Trim())
            .Where(key => key.Length > 0)
            .Take(120)
            .ToArray();

        if (name.Length is < 2 or > 60)
        {
            throw new ArgumentException("Düzen adı 2-60 karakter olmalıdır.", nameof(name));
        }

        if (normalizedKeys.Length < 2)
        {
            throw new ArgumentException("Düzende en az iki tuş bulunmalıdır.", nameof(keys));
        }

        lock (_syncRoot)
        {
            var layoutId = string.IsNullOrWhiteSpace(id) || BuiltInLayouts.Any(layout => layout.Id == id)
                ? Guid.NewGuid().ToString("N")
                : id;
            var definition = new KeyboardLayoutDefinition(layoutId, name, language, normalizedKeys);
            var index = _customLayouts.FindIndex(layout => layout.Id == layoutId);
            if (index >= 0)
            {
                _customLayouts[index] = definition;
            }
            else
            {
                _customLayouts.Add(definition);
            }

            Save();
        }
    }

    public void Remove(KeyboardLayoutDefinition definition)
    {
        if (definition.IsBuiltIn)
        {
            return;
        }

        lock (_syncRoot)
        {
            _customLayouts.RemoveAll(layout => layout.Id == definition.Id);
            Save();
        }
    }

    private void Save() => _store.Save(_customLayouts);

    private static bool IsValid(KeyboardLayoutDefinition layout) =>
        !string.IsNullOrWhiteSpace(layout.Id) &&
        !string.IsNullOrWhiteSpace(layout.Name) &&
        layout.Keys.Count >= 2;

    private static readonly IReadOnlyList<KeyboardLayoutDefinition> BuiltInLayouts =
    [
        new(
            "tr-q",
            "Türkçe Q",
            InputLanguage.Turkish,
            [
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                "q", "w", "e", "r", "t", "y", "u", "ı", "o", "p",
                "ğ", "ü", "a", "s", "d", "f", "g", "h", "j", "k",
                "l", "ş", "i", "z", "x", "c", "v", "b", "n", "m",
                "ö", "ç", ".", ",", "?", "!", "-", "'", "@", "_"
            ],
            true),
        new(
            "tr-f",
            "Türkçe F",
            InputLanguage.Turkish,
            [
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                "f", "g", "ğ", "ı", "o", "d", "r", "n", "h", "p",
                "q", "u", "i", "e", "a", "ü", "t", "k", "m", "l",
                "j", "ö", "v", "c", "ç", "z", "s", "b", ".", ","
            ],
            true),
        new(
            "en-qwerty",
            "English QWERTY",
            InputLanguage.English,
            [
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                "q", "w", "e", "r", "t", "y", "u", "i", "o", "p",
                "a", "s", "d", "f", "g", "h", "j", "k", "l", ";",
                "z", "x", "c", "v", "b", "n", "m", ".", ",", "?",
                "!", "-", "'", "\"", "@", "#", "$", "%", "&", "_"
            ],
            true),
        new(
            "symbols",
            "Semboller / Symbols",
            null,
            [
                "©", "®", "™", "°", "±", "×", "÷", "≈", "≠", "≤",
                "≥", "∞", "√", "∑", "∏", "π", "µ", "Ω", "←", "→",
                "↑", "↓", "↔", "•", "…", "—", "–", "“", "”", "‘",
                "’", "€", "£", "¥", "₺", "₿", "✓", "✗", "★", "☆"
            ],
            true),
        new(
            "emoji",
            "Emoji",
            null,
            [
                "😀", "😂", "😊", "😍", "🥰", "😎", "🤔", "😴", "😭", "😡",
                "👍", "👎", "👏", "🙏", "💪", "👋", "❤️", "💙", "💚", "🔥",
                "✨", "🎉", "✅", "❌", "⚠️", "💡", "📌", "📅", "📧", "☎️",
                "🚀", "🏆", "🎯", "🔒", "🔑", "🌍", "☀️", "🌙", "⭐", "💬"
            ],
            true)
    ];
}
