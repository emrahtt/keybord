# Yazım Asistanı 1.0 — Kullanım Kılavuzu / User Guide

## Kurulum / Installation

**Türkçe:** Sürümlü ZIP dosyasını yazma izniniz bulunan bir klasöre tamamen çıkarın ve `YazimAsistani.exe` dosyasını çalıştırın. Kurulum veya yönetici yetkisi gerekmez. Uygulama ayarlarını çalıştırılabilir dosyanın yanındaki `Data` klasörüne kaydeder; bu nedenle salt okunur `Program Files` klasörü önerilmez.

**English:** Extract the versioned ZIP into a writable folder and run `YazimAsistani.exe`. Installation and administrator access are not required. Settings are stored in the adjacent `Data` folder, so a read-only `Program Files` location is not recommended.

Windows bir SmartScreen uyarısı gösterirse dosyanın kaynağını ve sağlama toplamını doğrulayın. Bu proje kod imzalama sertifikası içermez.

## Temel kullanım / Basics

- `TR · Türkçe` / `EN · English`: yazım ve konuşma dilini değiştirir.
- `Ctrl + Alt + Space`: uygulama arka plandayken yazım dilini değiştirir.
- `Duraklat / Pause`: global önerileri geçici olarak kapatır.
- `Ekran klavyesi / On-screen keyboard`: Türkçe Q/F, İngilizce, simge, emoji veya özel düzeni açar.
- `Sesle yaz / Voice typing`: kurulu Windows konuşma paketini kullanarak çevrimdışı dikteyi başlatır.
- `Araçlar / Tools`: sözlük, snippet, düzen, kurtarma, yedek, AI ve metin araçlarını açar.

Öneri penceresinden bir kelime seçildiğinde yazdığınız bölüm değiştirilir ve seçim yerel sıralamaya öğretilir. Parola alanlarında öneri, snippet ve geçmiş yakalama otomatik olarak kapatılır.

When a suggestion is selected, the typed fragment is replaced and the choice is learned locally. Suggestions, snippets, and history capture are automatically disabled in password fields.

## Araçlar / Tools

### Sözlük / Dictionary

Yeni Türkçe veya İngilizce kelimeler ve virgülle ayrılmış alternatif yazımlar ekleyebilirsiniz. Arama kutusu ilk 500 eşleşmeyi gösterir.

Add Turkish or English terms with comma-separated aliases. The search box displays the first 500 matches.

### Kısaltmalar / Snippets

Bir tetikleyici ve genişletilmiş metin kaydedin. Örneğin `adr` tetikleyicisini adresinizle eşleştirirseniz `adr` yazıp boşluk tuşuna bastığınızda metin genişletilir.

Save a trigger and expansion. For example, mapping `addr` to your address expands it when you type `addr` followed by Space.

### Klavye düzenleri / Layouts

Hazır düzenlerden birini seçebilir veya tuşları boşluk, virgül ya da satır sonuyla ayırarak özel bir düzen oluşturabilirsiniz. Hazır düzenler silinemez; seçilip farklı adla kaydedilerek kopyalanabilir.

Choose a built-in layout or create one by separating keys with spaces, commas, or new lines. Built-in layouts cannot be deleted but can be copied under another name.

### Kurtarma / Recovery

Bu özellik gizlilik için varsayılan olarak kapalıdır. Etkinleştirildiğinde tamamlanan kelimeler ve cümleler portable `Data/writing-history.json` dosyasına yazılır. **Geçmişi sil / Clear** tüm kurtarma verisini kaldırır.

This feature is off by default for privacy. When enabled, completed words and sentences are saved to `Data/writing-history.json`. **Clear** removes all recovery data.

### Yedek / Backup

**Yedek oluştur** tüm `Data` klasörünü bir ZIP dosyasına yazar. **Geri yükle** yedekteki portable verileri doğrular ve aynı adlı geçerli verilerin üzerine yazar; ardından uygulamayı yeniden başlatın. API anahtarları Windows Kimlik Bilgisi Yöneticisi'nde olduğundan yedeğe dahil edilmez.

**Create backup** writes the full `Data` folder to a ZIP. **Restore** validates the backup and overwrites matching portable data; restart the application afterward. API keys are excluded because they live in Windows Credential Manager.

### AI ve bulut dikte / AI and cloud dictation

1. Google AI Studio'dan aldığınız Gemini API anahtarını girin ve **Anahtarı kaydet** düğmesine basın.
2. Metin araçları için giriş ve işlem türünü seçip **Çalıştır** düğmesine basın.
3. Bulut dikte için **Bulut ses kaydını başlat** düğmesine basın; ikinci basış kaydı durdurur, sesi Gemini'ye gönderir ve sonucu metin giriş alanına ekler.
4. Bulut özelliğini kapatmak için API anahtarını silebilirsiniz.

The API key is stored for the current Windows user, not in portable files. Text is sent only when **Run** is selected. Audio is sent only after the user starts and stops cloud dictation. Local voice typing from the main window does not use Gemini or the network.

## Sorun giderme / Troubleshooting

- **Öneriler görünmüyor:** Asistanın etkin olduğunu, doğru dili seçtiğinizi ve alanın parola alanı olmadığını denetleyin.
- **Hedef uygulamaya metin yazılamıyor:** Hedef yönetici olarak çalışıyorsa iki uygulamayı aynı yetki düzeyinde çalıştırın.
- **Yerel ses bulunamıyor:** Windows Ayarlar > Saat ve dil > Dil ve bölge bölümünden ilgili konuşma paketini kurun.
- **Mikrofon açılamıyor:** Windows mikrofon gizlilik iznini ve varsayılan kayıt aygıtını denetleyin.
- **Gemini hatası:** Anahtarı, model adını, internet erişimini, sağlayıcı kotasını ve desteklenen bölgeyi denetleyin.
- **Ayarlar kaydedilmiyor:** Uygulamayı yazma izniniz olan bir klasöre taşıyın.

## Kaldırma / Uninstallation

Önce sistem tepsisinden tamamen çıkın, ardından uygulama klasörünü silin. Gemini anahtarını kaldırmadan önce **Araçlar > AI > Anahtarı sil** düğmesini kullanın; aksi halde anahtar Windows Kimlik Bilgisi Yöneticisi'nde kalır.

Exit from the system tray, optionally delete the saved Gemini key from **Tools > AI**, and remove the application folder.
