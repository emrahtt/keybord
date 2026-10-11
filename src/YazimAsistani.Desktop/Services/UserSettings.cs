using YazimAsistani.Core;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed record UserSettings(
    bool AssistantEnabled = true,
    InputLanguage InputLanguage = InputLanguage.Turkish,
    string UiLanguage = "tr",
    bool SaveTypingHistory = false,
    string GeminiModel = "gemini-3.8-flash");

