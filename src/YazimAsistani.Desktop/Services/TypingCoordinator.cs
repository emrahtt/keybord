using System.Text;
using YazimAsistani.Core;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

internal sealed class TypingCoordinator(
    Func<bool> isEnabled,
    Func<string, string?, IReadOnlyList<Suggestion>> suggestionProvider,
    Func<string, string?> snippetProvider)
{
    private const uint VirtualKeyBackspace = 0x08;
    private const uint VirtualKeyTab = 0x09;
    private const uint VirtualKeyEnter = 0x0D;
    private const uint VirtualKeyEscape = 0x1B;
    private const uint VirtualKeySpace = 0x20;
    private const uint VirtualKeyEnd = 0x23;
    private const uint VirtualKeyHome = 0x24;
    private const uint VirtualKeyLeft = 0x25;
    private const uint VirtualKeyUp = 0x26;
    private const uint VirtualKeyRight = 0x27;
    private const uint VirtualKeyDown = 0x28;
    private const uint VirtualKeyDelete = 0x2E;

    private readonly StringBuilder _currentToken = new();
    private string? _previousWord;

    public event EventHandler<SuggestionsUpdatedEventArgs>? SuggestionsUpdated;

    public event EventHandler? SuggestionsDismissed;

    public event EventHandler<SnippetExpansionEventArgs>? SnippetExpansionRequested;

    public event EventHandler<TokenCompletedEventArgs>? TokenCompleted;

    public void HandleKey(GlobalKeyEventArgs key)
    {
        if (!isEnabled() || !InputSafetyService.CanOfferSuggestions())
        {
            Reset(clearPreviousWord: true);
            return;
        }

        switch (key.VirtualKey)
        {
            case VirtualKeyBackspace:
                if (_currentToken.Length > 0)
                {
                    _currentToken.Length--;
                    RefreshSuggestions();
                }
                else
                {
                    Dismiss();
                }

                return;
            case VirtualKeySpace:
                ExpandSnippetIfAvailable();
                CompleteCurrentToken();
                return;
            case VirtualKeyEnter:
                CompleteCurrentToken(endsSentence: true);
                return;
            case VirtualKeyTab:
            case VirtualKeyEscape:
            case VirtualKeyEnd:
            case VirtualKeyHome:
            case VirtualKeyLeft:
            case VirtualKeyUp:
            case VirtualKeyRight:
            case VirtualKeyDown:
            case VirtualKeyDelete:
                Reset(clearPreviousWord: false);
                return;
        }

        var translated = KeyboardTextTranslator.TryTranslate(key.VirtualKey, key.ScanCode);
        if (string.IsNullOrEmpty(translated))
        {
            return;
        }

        foreach (var character in translated)
        {
            if (char.IsLetterOrDigit(character) || character is '\'' or '’')
            {
                _currentToken.Append(character);
            }
            else
            {
                CompleteCurrentToken(character is '.' or '!' or '?');
            }
        }

        RefreshSuggestions();
    }

    public void AcceptSuggestion(string suggestion)
    {
        _currentToken.Clear();
        _currentToken.Append(suggestion);
        Dismiss();
    }

    private void RefreshSuggestions()
    {
        if (_currentToken.Length == 0)
        {
            Dismiss();
            return;
        }

        var token = _currentToken.ToString();
        var suggestions = suggestionProvider(token, _previousWord);
        if (suggestions.Count == 0 ||
            (suggestions.Count == 1 && string.Equals(
                suggestions[0].Text,
                token,
                StringComparison.CurrentCultureIgnoreCase)))
        {
            Dismiss();
            return;
        }

        SuggestionsUpdated?.Invoke(
            this,
            new SuggestionsUpdatedEventArgs(suggestions, _currentToken.Length));
    }

    private void CompleteCurrentToken(bool endsSentence = false)
    {
        if (_currentToken.Length > 0)
        {
            _previousWord = _currentToken.ToString();
            TokenCompleted?.Invoke(
                this,
                new TokenCompletedEventArgs(_previousWord, endsSentence));
        }

        _currentToken.Clear();
        Dismiss();
    }

    private void ExpandSnippetIfAvailable()
    {
        if (_currentToken.Length == 0)
        {
            return;
        }

        var trigger = _currentToken.ToString();
        var expansion = snippetProvider(trigger);
        if (!string.IsNullOrWhiteSpace(expansion))
        {
            SnippetExpansionRequested?.Invoke(
                this,
                new SnippetExpansionEventArgs(trigger, expansion, _currentToken.Length));
            _currentToken.Clear();
            _currentToken.Append(expansion);
        }
    }

    private void Reset(bool clearPreviousWord)
    {
        _currentToken.Clear();
        if (clearPreviousWord)
        {
            _previousWord = null;
        }

        Dismiss();
    }

    private void Dismiss() => SuggestionsDismissed?.Invoke(this, EventArgs.Empty);
}

internal sealed record SuggestionsUpdatedEventArgs(
    IReadOnlyList<Suggestion> Suggestions,
    int TypedCharacterCount);

internal sealed record SnippetExpansionEventArgs(
    string Trigger,
    string Expansion,
    int TypedCharacterCount);

internal sealed record TokenCompletedEventArgs(string Token, bool EndsSentence);

