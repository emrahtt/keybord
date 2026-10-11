# Yazım Asistanı 1.0

Türkçe ve İngilizce için çevrimdışı öncelikli, taşınabilir Windows yazım yardımcısı. Windows 10/11 x64 üzerinde kurulum gerektirmeden çalışır; temel yazım özellikleri internet olmadan kullanılabilir.

## Özellikler

- İmlecin yanında uygulamalar arası kelime tamamlama ve yazım düzeltme
- Türkçe karakter duyarlı, ASCII uyumlu öneriler (`cagri` → `çağrı`)
- Kullanıma göre uyarlanan sıralama ve yerel sonraki-kelime öğrenmesi
- Parola ve gizli giriş alanlarında otomatik devre dışı kalma
- Türkçe/İngilizce arayüz ve `Ctrl + Alt + Space` dil kısayolu
- Türkçe Q, Türkçe F, İngilizce, simge ve emoji ekran klavyeleri
- Kullanıcı tarafından oluşturulabilen özel klavye düzenleri
- Kullanıcı sözlüğü, alternatif yazımlar ve kısaltma/snippet genişletme
- Windows konuşma paketleriyle tamamen yerel sesle yazma
- İsteğe bağlı Gemini bulut transkripsiyonu
- İsteğe bağlı Gemini ile düzeltme, sadeleştirme, resmîleştirme, özet ve çeviri
- Varsayılan olarak kapalı yazı kurtarma/geçmiş özelliği
- Portable `Data` klasörü için ZIP yedekleme ve geri yükleme
- Telemetri ve reklam içermez

## Hızlı başlangıç

1. Final ZIP dosyasını yazma izniniz bulunan bir klasöre çıkarın.
2. `YazimAsistani.exe` dosyasını çalıştırın.
3. Yazım dilini `TR` veya `EN` olarak seçin.
4. Başka bir uygulamada yazmaya başlayın; öneriler imlecin yanında görünür.
5. Ayrıntılı ayarlar için **Araçlar / Tools** bölümünü açın.

Kapat düğmesi uygulamayı sistem tepsisine küçültür. Tamamen çıkmak için tepsi menüsündeki **Çıkış / Exit** komutunu kullanın.

## Geliştirme

Gereksinim: .NET 8 SDK.

```powershell
dotnet restore YazimAsistani.sln
dotnet test YazimAsistani.sln -c Release
dotnet build YazimAsistani.sln -c Release
./scripts/publish-portable.ps1
```

Betik, `artifacts/portable-win-x64` klasörünü ve sürümlü ZIP dosyasını üretir. Windows GitHub Actions iş akışı derleme, test ve portable yayın adımlarını tekrarlar.

## Önemli notlar

- Global klavye, mikrofon ve metin gönderme davranışları gerçek Windows 10/11 üzerinde doğrulanmalıdır.
- Yerel sesle yazma, Windows'ta ilgili Türkçe veya İngilizce konuşma paketinin kurulu olmasını gerektirir.
- Yönetici olarak çalışan bir hedef uygulamaya metin göndermek için Yazım Asistanı'nın da aynı yetki düzeyinde çalışması gerekebilir.
- Gemini özellikleri yalnızca kullanıcı API anahtarı kaydedip ilgili düğmeye bastığında ağ isteği yapar ve sağlayıcının kullanım koşulları ile olası ücretlerine tabidir.

Ayrıntılı kullanım için [Kullanım Kılavuzu](docs/USER_GUIDE.md), veri işleme ayrıntıları için [Gizlilik](docs/PRIVACY.md) belgesine bakın.

## English

Yazım Asistanı is an offline-first, portable Windows writing assistant for Turkish and English. It provides system-wide suggestions, adaptive local learning, on-screen keyboards, snippets, local Windows dictation, opt-in Gemini text/audio tools, recovery, and portable backup. See the bilingual [User Guide](docs/USER_GUIDE.md) for setup and usage.
