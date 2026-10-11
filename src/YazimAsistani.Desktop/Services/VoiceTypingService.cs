using System.Globalization;
using System.Speech.Recognition;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed class VoiceTypingService : IDisposable
{
    private SpeechRecognitionEngine? _recognizer;

    public event EventHandler<string>? TextRecognized;

    public bool IsRunning => _recognizer is not null;

    public void Start(InputLanguage language)
    {
        if (IsRunning)
        {
            return;
        }

        var preferredCultures = language == InputLanguage.Turkish
            ? new[] { "tr-TR" }
            : new[] { "en-US", "en-GB" };
        var installedRecognizer = SpeechRecognitionEngine
            .InstalledRecognizers()
            .FirstOrDefault(recognizer => preferredCultures.Contains(
                recognizer.Culture.Name,
                StringComparer.OrdinalIgnoreCase));

        if (installedRecognizer is null)
        {
            var languageName = language == InputLanguage.Turkish ? "Türkçe" : "English";
            throw new InvalidOperationException(
                $"{languageName} konuşma tanıyıcısı yüklü değil. Windows dil ayarlarından konuşma paketini yükleyin.");
        }

        var recognizer = new SpeechRecognitionEngine(installedRecognizer);
        recognizer.LoadGrammar(new DictationGrammar());
        recognizer.SpeechRecognized += HandleSpeechRecognized;
        recognizer.SetInputToDefaultAudioDevice();
        recognizer.RecognizeAsync(RecognizeMode.Multiple);
        _recognizer = recognizer;
    }

    public void Stop()
    {
        var recognizer = _recognizer;
        if (recognizer is null)
        {
            return;
        }

        _recognizer = null;
        recognizer.SpeechRecognized -= HandleSpeechRecognized;
        try
        {
            recognizer.RecognizeAsyncCancel();
        }
        catch (InvalidOperationException)
        {
            // The recognizer may already have stopped after an audio-device change.
        }
        finally
        {
            recognizer.Dispose();
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void HandleSpeechRecognized(object? sender, SpeechRecognizedEventArgs eventArgs)
    {
        if (eventArgs.Result.Confidence < 0.45f || string.IsNullOrWhiteSpace(eventArgs.Result.Text))
        {
            return;
        }

        TextRecognized?.Invoke(this, eventArgs.Result.Text.Trim());
    }
}
