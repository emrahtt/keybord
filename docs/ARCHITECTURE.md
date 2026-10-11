# Mimari

## Katmanlar

- `YazimAsistani.Core`: Platformdan bağımsız normalizasyon, sözlük, öneri, düzeltme ve sonraki kelime motoru.
- `YazimAsistani.Desktop`: WPF arayüzü, sistem tepsisi, global kısayollar ve Windows metin giriş köprüsü.
- `YazimAsistani.Core.Tests`: Dil motorunun hızlı ve çevrimdışı birim testleri.

## Temel ilkeler

1. Yerel özellikler ağ olmadan çalışır.
2. Gemini metin ve ses özellikleri ayrı bir adaptördedir; yerel çekirdek bunlara bağımlı değildir ve varsayılan akışta ağ isteği yapmaz.
3. Kullanıcı sözlüğü ile öğrenilmiş sıklıklar taşınabilir `Data` dizisindedir.
4. Global yakalama parola alanlarında ve uygulama duraklatıldığında devre dışıdır.
5. Başka ürünlerin marka, kod, ikili dosya veya lisansı belirsiz sözlükleri kullanılmaz.

## Veri ve dış bağımlılıklar

- Portable JSON verileri `Data` altında; Gemini anahtarı Windows Kimlik Bilgisi Yöneticisi'ndedir.
- Global klavye ve metin gönderme Win32 katmanında; parola alanı denetimi UI Automation katmanındadır.
- Yerel dikte `System.Speech`, isteğe bağlı WAV kaydı `NAudio` ve bulut işlemleri Gemini REST API ile sağlanır.
- WPF/Win32 dışındaki öneri motoru platformdan bağımsızdır ve birim testleri Linux ile Windows'ta çalışabilir.

