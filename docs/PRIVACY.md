# Gizlilik / Privacy

## Yerel çalışma / Local operation

- Telemetri, reklam ve otomatik hata raporlama yoktur.
- Yazım önerileri, sözlük, snippet, klavye düzenleri ve yerel sonraki-kelime öğrenmesi cihaz üzerinde çalışır.
- Yerel Windows sesle yazma özelliği Gemini'yi veya uygulamanın kendi ağ istemcisini kullanmaz.
- Parola/gizli giriş alanlarında global öneri, snippet ve geçmiş işleme otomatik olarak devre dışı bırakılır.
- Yazı kurtarma/geçmiş özelliği varsayılan olarak kapalıdır ve kullanıcı tarafından temizlenebilir.

Portable kullanıcı verileri uygulamanın yanındaki `Data` klasöründe JSON olarak saklanır. Bu klasör ayarlar, öğrenilmiş kelimeler, snippet'ler, özel klavye düzenleri ve kullanıcı etkinleştirdiyse yazı geçmişi içerir. ZIP yedekleri bu verilerin tamamını içerir; kullanıcı yedek dosyasını güvenli tutmalıdır.

There is no telemetry, advertising, or automatic crash reporting. Core suggestions, dictionaries, snippets, layouts, learning, and Windows local dictation operate on the device. Portable JSON data is stored in the adjacent `Data` folder. Writing recovery is off by default.

## İsteğe bağlı bulut özellikleri / Optional cloud features

Gemini özellikleri varsayılan olarak çalışmaz ve bir API anahtarı gerektirir. Kullanıcı:

- bir metin işlemi için **Çalıştır / Run** düğmesine bastığında seçili metin ve komut;
- bulut dikte kaydını başlatıp durdurduğunda WAV ses kaydı

Google Gemini API'ye gönderilir. Sağlayıcının kendi gizlilik, saklama, kota ve ücret kuralları geçerlidir. Uygulama bu istekleri kendi sunucusundan geçirmez.

Gemini features are opt-in and require an API key. Selected text is sent only after **Run** is clicked. Recorded WAV audio is sent only after cloud dictation is explicitly started and stopped. Requests go directly to the Google Gemini API; Google policies, retention, quotas, and charges apply.

API anahtarı portable ayarlara veya yedeklere yazılmaz. Geçerli Windows kullanıcısına bağlı Windows Kimlik Bilgisi Yöneticisi'nde saklanır ve **Anahtarı sil / Delete** ile kaldırılabilir.

The API key is stored in Windows Credential Manager for the current Windows user, not in portable settings or backups.
