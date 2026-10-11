using System.Net.Http;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed partial class GeminiTextService : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
        Timeout = TimeSpan.FromSeconds(75)
    };

    public async Task<string> TransformAsync(
        string apiKey,
        string model,
        AiTextAction action,
        string input,
        string? customInstruction,
        CancellationToken cancellationToken = default)
    {
        ValidateCredentials(apiKey, model);

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Dönüştürülecek metin boş olamaz.", nameof(input));
        }

        var instruction = BuildInstruction(action, customInstruction);
        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = instruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = input } }
                }
            },
            generationConfig = new
            {
                temperature = 0.25,
                maxOutputTokens = 4096
            }
        };

        return await SendAsync(apiKey, model, requestBody, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> TranscribeAudioAsync(
        string apiKey,
        string model,
        byte[] waveAudio,
        InputLanguage language,
        CancellationToken cancellationToken = default)
    {
        ValidateCredentials(apiKey, model);
        if (waveAudio.Length == 0)
        {
            throw new ArgumentException("Ses kaydı boş.", nameof(waveAudio));
        }

        if (waveAudio.Length > 14_000_000)
        {
            throw new ArgumentException(
                "Ses kaydı bulut isteği için çok büyük. Daha kısa bir kayıt deneyin.",
                nameof(waveAudio));
        }

        var languageName = language == InputLanguage.Turkish ? "Turkish" : "English";
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            text = $"Transcribe this audio in {languageName}. Preserve punctuation and casing. Return only the transcription."
                        },
                        new
                        {
                            inlineData = new
                            {
                                mimeType = "audio/wav",
                                data = Convert.ToBase64String(waveAudio)
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 4096
            }
        };

        return await SendAsync(apiKey, model, requestBody, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendAsync(
        string apiKey,
        string model,
        object requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini isteği başarısız oldu ({(int)response.StatusCode}). API anahtarını ve model adını denetleyin.");
        }

        using var document = JsonDocument.Parse(responseJson);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0 ||
            !candidates[0].TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Gemini yanıtında kullanılabilir metin bulunamadı.");
        }

        var texts = parts.EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString())
            .Where(text => !string.IsNullOrWhiteSpace(text));
        var result = string.Join(Environment.NewLine, texts).Trim();
        if (result.Length == 0)
        {
            throw new InvalidDataException("Gemini yanıtında kullanılabilir metin bulunamadı.");
        }

        return result;
    }

    private static void ValidateCredentials(string apiKey, string model)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API anahtarı kayıtlı değil.");
        }

        if (!ModelNameRegex().IsMatch(model))
        {
            throw new ArgumentException("Gemini model adı geçersiz.", nameof(model));
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string BuildInstruction(AiTextAction action, string? customInstruction) => action switch
    {
        AiTextAction.Correct => "Correct spelling, punctuation, and grammar. Preserve the original language and meaning. Return only the corrected text.",
        AiTextAction.Simplify => "Rewrite the text in clear, simple language. Preserve the original language and meaning. Return only the rewritten text.",
        AiTextAction.Formalize => "Rewrite the text in a professional and formal tone. Preserve the original language and meaning. Return only the rewritten text.",
        AiTextAction.Summarize => "Summarize the text concisely in the same language. Return only the summary.",
        AiTextAction.TranslateToTurkish => "Translate the text naturally into Turkish. Return only the translation.",
        AiTextAction.TranslateToEnglish => "Translate the text naturally into English. Return only the translation.",
        AiTextAction.Custom when !string.IsNullOrWhiteSpace(customInstruction) =>
            $"Follow this instruction: {customInstruction.Trim()} Return only the resulting text.",
        _ => throw new ArgumentException("Özel AI komutu boş olamaz.", nameof(customInstruction))
    };

    [GeneratedRegex("^[a-zA-Z0-9._-]{2,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex ModelNameRegex();
}

public enum AiTextAction
{
    Correct,
    Simplify,
    Formalize,
    Summarize,
    TranslateToTurkish,
    TranslateToEnglish,
    Custom
}
