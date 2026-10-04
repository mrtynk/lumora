# Lumora Geliştirme Planı

Bu plan, Lumora'nın kalan geliştirmelerini bağımsız ve doğrulanabilir GitHub issue'larına böler. Issue'lar sprint sırasına göre ele alınmalı; her issue ayrı branch ve Pull Request üzerinden tamamlanmalıdır.

## Sprint 1 — Unity Temel Sahne ve Backend Bağlantısı

### Issue 1: Unity Prototype Scene Builder

**Kısa amaç:** Tekrarlanabilir bir temel 3D prototip sahnesini otomatik hazırlamak.

**Kapsam:** Zemin, oyuncu, kamera, ışık, temel hareket bileşenleri ve test amaçlı `PuzzlePaper` objesini oluşturan Unity Editor sahne kurulum aracı.

**Beklenen çıktı:** Tek komutla veya Editor menüsüyle çalışan, oynanabilir prototip sahnesi ve gerekli Unity scriptleri.

**Kabul kriterleri:**

- [ ] Araç boş bir sahnede gerekli objeleri oluşturuyor.
- [ ] Oyuncu sahnede hareket edebiliyor ve zemin üzerinde kalıyor.
- [ ] Kamera, ışık ve `PuzzlePaper` objesi doğru şekilde hazırlanıyor.
- [ ] Aynı araç tekrar çalıştırıldığında gereksiz kopyalar üretmiyor.

### Issue 2: Unity'den Backend'e Event Gönderme

**Kısa amaç:** Unity etkileşimlerini Lumora backend'ine JSON event olarak göndermek.

**Kapsam:** `GameEventSender`, puzzle yaklaşma alanı ve E tuşuyla `puzzle_started` eventinin `POST /api/events` endpoint'ine gönderilmesi.

**Beklenen çıktı:** Unity Console'da sonucu görülebilen ve PostgreSQL'e kaydedilen örnek puzzle eventi.

**Kabul kriterleri:**

- [ ] Gönderilen JSON mevcut API alanlarıyla uyumlu.
- [ ] Başarılı ve başarısız istekler Unity Console'da anlaşılır biçimde gösteriliyor.
- [ ] Event yalnızca amaçlanan etkileşim gerçekleştiğinde gönderiliyor.
- [ ] Kayıt `GET /api/events` ile doğrulanabiliyor.

## Sprint 2 — Puzzle Sistemi

### Issue 3: Puzzle Popup UI

**Kısa amaç:** Oyuncu puzzle ile etkileşime girdiğinde açılan ortak bir puzzle arayüzü oluşturmak.

**Kapsam:** Açma/kapatma davranışı, başlık, açıklama, içerik alanı, kapatma butonu ve oyuncu hareketinin popup açıkken yönetilmesi.

**Beklenen çıktı:** Farklı puzzle türleri tarafından tekrar kullanılabilen Unity UI prefabı ve kontrol scripti.

**Kabul kriterleri:**

- [ ] Popup etkileşimle açılıyor ve güvenli biçimde kapanıyor.
- [ ] Açıkken oyun girdileri çakışmıyor.
- [ ] UI farklı ekran oranlarında kullanılabilir kalıyor.
- [ ] Puzzle içeriği koddan veya Inspector'dan değiştirilebiliyor.

### Issue 4: Hidden Object Demo

**Kısa amaç:** Çocuğun görsel içindeki hedef nesneleri bulduğu oynanabilir bir demo hazırlamak.

**Kapsam:** Hedef nesne listesi, doğru/yanlış tıklama, tamamlanma durumu ve ilgili eventlerin gönderilmesi.

**Beklenen çıktı:** En az üç hedef içeren, başlatılıp tamamlanabilen hidden object prototipi.

**Kabul kriterleri:**

- [ ] Doğru bulunan nesneler bir kez sayılıyor.
- [ ] Yanlış tıklamalar `wrong_click` eventi üretiyor.
- [ ] Tüm hedefler bulununca `puzzle_solved` eventi gönderiliyor.
- [ ] Yeniden başlatıldığında puzzle durumu temizleniyor.

### Issue 5: Memory Match Demo

**Kısa amaç:** Eş kartları bulmaya dayalı basit bir hafıza puzzle'ı geliştirmek.

**Kapsam:** Kart üretimi, karıştırma, iki kart seçme, eşleşme kontrolü, deneme sayısı ve bitiş eventi.

**Beklenen çıktı:** En az üç çift kartla çalışan tekrar oynanabilir memory match demosu.

**Kabul kriterleri:**

- [ ] Kartlar her oyun başında karıştırılıyor.
- [ ] Aynı anda ikiden fazla kart açılamıyor.
- [ ] Yanlış eşleşme tekrar deneme olarak izleniyor.
- [ ] Tüm çiftler bulunduğunda `puzzle_solved` eventi gönderiliyor.

### Issue 6: Pattern Puzzle Demo

**Kısa amaç:** Çocuğun basit bir şekil veya renk dizisini tamamladığı puzzle geliştirmek.

**Kapsam:** Örüntü gösterimi, seçenekler, doğru/yanlış cevap, yeniden deneme ve tamamlanma eventi.

**Beklenen çıktı:** En az üç örnek örüntüyle çalışan pattern puzzle prototipi.

**Kabul kriterleri:**

- [ ] Seçenekler açık ve tıklanabilir biçimde gösteriliyor.
- [ ] Doğru ve yanlış cevap geri bildirimi veriliyor.
- [ ] Yeniden denemeler event olarak kaydediliyor.
- [ ] Doğru cevapta `puzzle_solved` eventi gönderiliyor.

## Sprint 3 — Oyun Akışı

### Issue 7: Işıklı Vadi Bölüm Akışı

**Kısa amaç:** Işıklı Vadi bölümünün başlangıçtan ödüle kadar oynanabilir akışını kurmak.

**Kapsam:** Bölüm başlangıcı, keşif noktaları, puzzle sıralaması, ilerleme koşulları, bölüm sonu ve ödül adımı.

**Beklenen çıktı:** Oyuncunun kesintisiz tamamlayabildiği temel bir bölüm akışı.

**Kabul kriterleri:**

- [ ] Bölüm adımları doğru sırada açılıyor.
- [ ] Gerekli puzzle tamamlanmadan sonraki adım açılmıyor.
- [ ] Keşif ve ödül eventleri doğru bölge bilgisiyle gönderiliyor.
- [ ] Bölüm baştan sona bloklayıcı hata olmadan tamamlanıyor.

### Issue 8: NPC Etkileşim Sistemi

**Kısa amaç:** Oyuncunun NPC'lerle konuşmasını ve seçim yapmasını sağlayan ortak sistem geliştirmek.

**Kapsam:** Yaklaşma algılama, diyalog UI'ı, seçenekler, yardım davranışı ve event gönderimi.

**Beklenen çıktı:** En az bir örnek NPC ile tekrar kullanılabilir diyalog ve seçim sistemi.

**Kabul kriterleri:**

- [ ] NPC etkileşimi yalnızca oyuncu yakındayken başlıyor.
- [ ] Diyalog seçenekleri seçilebiliyor ve akışı ilerletiyor.
- [ ] `dialogue_selected`, `choice_made` ve uygun durumda `npc_helped` eventleri gönderiliyor.
- [ ] Sistem yeni NPC ve diyalog eklemeye uygun yapılandırılmış.

## Sprint 4 — Backend İyileştirme

### Issue 9: Event Filtreleme

**Kısa amaç:** Kayıtlı eventlerin ihtiyaç duyulan ölçütlere göre sorgulanmasını sağlamak.

**Kapsam:** `GET /api/events` için childId, eventType, region ve tarih aralığı filtreleri; mevcut varsayılan davranışın korunması.

**Beklenen çıktı:** İsteğe bağlı query parametreleriyle filtrelenebilen event endpoint'i ve doğrulama testleri.

**Kabul kriterleri:**

- [ ] Filtre verilmediğinde mevcut response formatı ve davranış korunuyor.
- [ ] Desteklenen filtreler tek başına ve birlikte çalışıyor.
- [ ] Parametreler güvenli, parametrik sorgularla işleniyor.
- [ ] Geçersiz filtre girdileri anlaşılır hata cevabı üretiyor.

### Issue 10: Skor Mantığını İyileştirme

**Kısa amaç:** Altı beceri skorunu eventlere dayalı daha dengeli ve açıklanabilir kurallarla hesaplamak.

**Kapsam:** Mevcut kuralların belgelenmesi, puan ağırlıkları, alt/üst sınırlar ve örnek event dizileri için testler.

**Beklenen çıktı:** Deterministik skor kuralları, test senaryoları ve korunan endpoint response formatı.

**Kabul kriterleri:**

- [ ] Altı becerinin her biri belgelenmiş event kurallarıyla hesaplanıyor.
- [ ] Skorlar tanımlanan minimum ve maksimum aralıkta kalıyor.
- [ ] Aynı event verisi her zaman aynı sonucu üretiyor.
- [ ] `GET /api/scores/:childId` response formatı bozulmuyor.

## Sprint 5 — Veli Paneli Geliştirme

### Issue 11: Event Tablosu

**Kısa amaç:** Çocuğa ait oyun eventlerini veli panelinde okunabilir bir tabloda göstermek.

**Kapsam:** Event türü, bölge, puzzle türü, değer ve tarih alanları; yükleniyor, boş ve hata durumları.

**Beklenen çıktı:** API verisiyle çalışan responsive event tablosu.

**Kabul kriterleri:**

- [ ] Eventler en yeni kayıt üstte olacak şekilde gösteriliyor.
- [ ] Yükleniyor, boş veri ve API hatası durumları anlaşılır.
- [ ] Uzun değerler panel düzenini bozmuyor.
- [ ] Mevcut özet ve skor alanları çalışmaya devam ediyor.

### Issue 12: Skor Grafik Alanı

**Kısa amaç:** Altı beceri skorunu velilerin kolayca karşılaştırabileceği görsel bir alanda sunmak.

**Kapsam:** Uygun grafik türü, beceri etiketleri, değer gösterimi, responsive tasarım ve erişilebilir metin karşılığı.

**Beklenen çıktı:** Mevcut skor endpoint'ini kullanan, altı beceriyi gösteren grafik bileşeni.

**Kabul kriterleri:**

- [ ] Altı becerinin tamamı doğru değerlerle gösteriliyor.
- [ ] Grafik mobil ve masaüstü genişliklerinde okunabiliyor.
- [ ] Grafik verisi olmadığında uygun boş durum gösteriliyor.
- [ ] Görsel bilgi metin veya etiketlerle de erişilebilir.

## Sprint 6 — Demo ve Teslim Hazırlığı

### Issue 13: README Hazırlama

**Kısa amaç:** Projeyi değerlendiren veya geliştiren kişinin sistemi kolayca kurup çalıştırabilmesini sağlamak.

**Kapsam:** Proje özeti, mimari, gereksinimler, backend/veritabanı/panel/Unity kurulumları, çalıştırma ve demo akışı.

**Beklenen çıktı:** Proje kökünde güncel, sade ve uygulanabilir bir `README.md`.

**Kabul kriterleri:**

- [ ] Tüm teknoloji katmanları ve klasör yapısı açıklanıyor.
- [ ] Kurulum ve çalıştırma komutları yeni bir ortamda uygulanabilir.
- [ ] Gerekli environment değişkenleri gizli değer paylaşmadan belgeleniyor.
- [ ] API endpointleri ve temel demo senaryosu yer alıyor.

### Issue 14: Demo Verisi Scripti

**Kısa amaç:** Veli paneli ve analitik motorunu göstermek için tekrarlanabilir örnek çocuk eventleri üretmek.

**Kapsam:** `demo-child-001` için anlamlı event sırası, çalıştırma komutu, hata yönetimi ve tekrar çalıştırma davranışı.

**Beklenen çıktı:** Backend API üzerinden örnek eventler oluşturan belgelenmiş bir seed/demo scripti.

**Kabul kriterleri:**

- [ ] Script desteklenen event tiplerinden dengeli bir demo dizisi oluşturuyor.
- [ ] Her isteğin başarı veya hata durumu görünür biçimde raporlanıyor.
- [ ] Oluşan eventler ve altı beceri skoru API üzerinden doğrulanabiliyor.
- [ ] Çalıştırma yöntemi README veya ilgili dokümanda açıklanıyor.

## Önerilen ilerleme sırası

İlk olarak **Issue 1: Unity Prototype Scene Builder** ele alınmalıdır. Bu görev, sonraki Unity görevlerinin üzerinde geliştirileceği standart ve tekrar üretilebilir sahneyi hazırlar. Ardından Issue 2 ile backend event bağlantısı doğrulanmalı ve Sprint 2 puzzle görevlerine geçilmelidir.
