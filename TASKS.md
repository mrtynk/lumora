# Lumora Proje Yol Haritası

> Codex’e görev verirken örnek: **“TASKS.md içindeki 1.4 görevini uygula, CODEX_PROMPT.md kurallarına uy.”**

Her görev ayrı branch/PR üzerinde ve yalnızca belirtilen kapsamda yapılır. Durum değerleri: `Yapılacak`, `Devam Ediyor`, `Tamamlandı`, `Daha Sonra`. Ortak kurallar için `CODEX_PROMPT.md` esas alınır.

## Oyun vizyonu

Lumora; 5–7 yaş çocuklara yönelik, basit grafikli ve tamamlanabilir hikâye tabanlı bir 3D mobil macera oyunudur. Işık Ağacı gücünü kaybetmiş, dört ışık tohumu farklı bölgelere dağılmıştır. Oyuncu bölgeleri sırayla keşfeder, ana ışık tohumunu toplar ve bir sonraki bölgenin portalını açar.

> Mevcut puzzle sistemleri ana oyun içinde oyun mantığında değil, gizli/opsiyonel mini görevler olarak kullanılacaktır. Ana oyun ilerleyişi 4 bölge, ışık tohumu toplama ve portal açma sistemi üzerine kurulacaktır.

## 1. Unity Oyun Prototipi ve Ana Oynanış

### 1.1 Teknik Sahne ve Event Altyapısı

**Durum:** Tamamlandı  
**Amaç:** Unity sahnesi, oyuncu, event gönderimi ve temel demo altyapısını kurmak.  
**Kapsam:** Tamamlanan `game/Assets` teknik prototipi.  
**Çıktı:** Prototype Scene Builder; Unity Backend Event Sender; Puzzle Popup UI; Işıklı Vadi Demo Akışı (teknik event demosu).  
**Eventler:** `area_explored`, `reward_collected` ve mevcut temel oyun eventleri.  
**Test:** PrototypeScene otomatik kurulur; hareket, backend bağlantısı ve teknik demo akışı doğrulanır.  
**Not:** Bu görev teknik event ve mini görev altyapısını sağlar; nihai oyun yapısı için bölge, portal, ışık tohumu ve hikâye ilerleme sistemi ayrıca geliştirilecektir. Bu bölüm gerçek oyun deneyimini değil, teknik altyapıyı temsil eder.

### 1.2 Mini Puzzle Altyapısı

**Durum:** Tamamlandı  
**Amaç:** Hidden Object, Memory Match ve Pattern Puzzle sistemlerini kurmak.  
**Kapsam:** Tamamlanan puzzle UI ve event altyapısı.  
**Çıktı:** Hidden Object Demo Puzzle; Memory Match Demo Puzzle; Pattern Puzzle Demo.  
**Eventler:** `puzzle_started`, `puzzle_solved`, `puzzle_failed`, `puzzle_abandoned`, `wrong_click`, `hint_requested`, `retry_attempt`, `choice_made`.  
**Test:** Üç puzzle açılır, tamamlanır ve eventleri API üzerinden doğrulanır.  
**Not:** Bu görev teknik event ve mini görev altyapısını sağlar; nihai oyun yapısı için bölge, portal, ışık tohumu ve hikâye ilerleme sistemi ayrıca geliştirilecektir. Puzzlelar ana ilerleyişin merkezinde değil, gizli/opsiyonel davranış verisi noktalarıdır.

### 1.3 NPC ve Seçim Altyapısı

**Durum:** Tamamlandı  
**Amaç:** Basit NPC diyaloğu ve seçim eventlerini kurmak.  
**Kapsam:** Tamamlanan NPC ve seçim prototipi.  
**Çıktı:** NPC ve Seçim Etkileşimi.  
**Eventler:** `dialogue_selected`, `choice_made`, `npc_helped`.  
**Test:** Yardım Et, Sonra ve Kapat akışları Play Mode’da doğrulanır.  
**Not:** Bu görev teknik event ve mini görev altyapısını sağlar; nihai oyun yapısı için bölge, portal, ışık tohumu ve hikâye ilerleme sistemi ayrıca geliştirilecektir. Bu yapı ileride bölge görevleri ve hikâye seçimleri için kullanılacaktır.

### 1.4 Ana Hikâye Giriş Sahnesi

**Durum:** Yapılacak  
**Amaç:** Lumora dünyasının karardığını ve Işık Ağacı’nın gücünü kaybettiğini anlatan kısa giriş akışı oluşturmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Kısa giriş UI/metni; güç kaybeden Işık Ağacı sahnesi; “Işıklı Vadi’ye git ve ilk ışık tohumunu bul.” hedefi.  
**Eventler:** `area_explored`.  
**Test:** Play Mode başladığında oyuncu hikâyeyi ve ilk hedefi anlayabilmeli.  
**Not:** Giriş kısa, çocuk dostu ve atlanabilir olmalı.

### 1.5 Bölge Sistemi ve Portal Mantığı

**Durum:** Yapılacak  
**Amaç:** Dört bölgeye dayalı sıralı ilerleme sistemini kurmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Işıklı Vadi, Sisli Orman, Kristal Mağara ve Karanlık Tepe temsilleri; portal noktaları; tohuma bağlı kilit/açık durumu.  
**Eventler:** `area_explored`, `reward_collected`.  
**Test:** İlk bölge tamamlanmadan ikinci bölgeye geçilememeli; tohum alınınca portal açılmalı.  
**Not:** Kalıcı kayıt bu görevin dışında; Play Mode oturum durumu yeterlidir.

### 1.6 Işık Tohumu Toplama Sistemi

**Durum:** Yapılacak  
**Amaç:** Her bölgenin ana hedefi olan ışık tohumlarını toplanabilir yapmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Işık tohumu objesi; bölgeye ait tohum durumu; UI güncellemesi; portal açma bildirimi.  
**Eventler:** `reward_collected`.  
**Test:** Tohum alındığında event gitmeli, UI güncellenmeli ve sonraki portal açılmalı.  
**Not:** Her bölgenin tohumu yalnızca bir kez toplanabilmeli.

### 1.7 Basit Harita / Bölge İlerleme UI

**Durum:** Yapılacak  
**Amaç:** Oyuncunun bölgesini, hedefini ve topladığı ışık tohumu sayısını göstermek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Bölge adı; tohum sayacı; aktif hedef; portal kilit/açık göstergesi.  
**Eventler:** Yok.  
**Test:** Bölge, tohum ve portal durumu değiştikçe UI doğru güncellenmeli.  
**Not:** Mobil ekranda okunabilir ve sade olmalı.

### 1.8 Bölge 1 — Işıklı Vadi Oynanabilir Level

**Durum:** Yapılacak  
**Amaç:** Teknik demo alanını küçük fakat anlamlı ilk bölgeye dönüştürmek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Başlangıç noktası; yürünebilir yol/platformlar; Işık Ağacı atmosferi; ışık tohumu; portal; NPC; gizli puzzle noktası.  
**Eventler:** `area_explored`, `reward_collected`, `npc_helped`.  
**Test:** Oyuncu başlangıçtan tohuma ulaşabilmeli ve tohum alındığında portal açılmalı.  
**Not:** Kısa bir oynanabilir level hedeflenir; büyük açık dünya yapılmaz.

### 1.9 Bölge 2 — Sisli Orman Taslağı

**Durum:** Yapılacak  
**Amaç:** İkinci bölge için küçük oynanabilir alan taslağı oluşturmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Sisli Orman teması; ışık tohumu; giriş/çıkış portalı; basit engel veya keşif yolu.  
**Eventler:** `area_explored`, `reward_collected`.  
**Test:** Bölge 1 tamamlanmadan erişilememeli; tohum alınca sonraki portal açılmalı.  
**Not:** Primitive ve düşük maliyetli görseller yeterlidir.

### 1.10 Bölge 3 — Kristal Mağara Taslağı

**Durum:** Yapılacak  
**Amaç:** Üçüncü bölge için küçük oynanabilir alan taslağı oluşturmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Kristal Mağara teması; ışık tohumu; basit platform/geçiş yapısı; portal.  
**Eventler:** `area_explored`, `reward_collected`.  
**Test:** Bölge 2 tamamlanmadan erişilememeli; tohum alınca son bölge açılmalı.  
**Not:** Bölge mekaniği tek oturumda test edilebilir boyutta kalmalı.

### 1.11 Bölge 4 — Karanlık Tepe ve Final Taslağı

**Durum:** Yapılacak  
**Amaç:** Son bölgeyi ve Işık Ağacı’nın geri kazanım finalini oluşturmak.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Karanlık Tepe alanı; son ışık tohumu; final Işık Ağacı mesajı; demo finali.  
**Eventler:** `area_explored`, `reward_collected`, `choice_made`.  
**Test:** Dördüncü tohumdan sonra final mesajı gösterilmeli.  
**Not:** Final kısa ve açık bir tamamlanma hissi vermeli.

### 1.12 Portal Açılma / Kilit Sistemi

**Durum:** Yapılacak  
**Amaç:** Bölge geçişlerini ilgili ışık tohumu durumuna göre kilitlemek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Kapalı/açık portal görünümü; kilit mesajı; açık portal ile bölge geçişi.  
**Eventler:** `area_explored`.  
**Test:** Tohum yokken geçiş engellenmeli; tohum varken sonraki bölgeye geçilmeli.  
**Not:** 1.5 ve 1.6 sistemleriyle tek bir durum kaynağı kullanılmalı.

### 1.13 Mini Puzzle Noktalarını Haritaya Yerleştirme

**Durum:** Yapılacak  
**Amaç:** Mevcut puzzleları gizli ve opsiyonel davranış verisi noktalarına dönüştürmek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Her bölgede en az bir opsiyonel puzzle noktası; ana ilerleyişten bağımsız yerleşim; isteğe bağlı küçük ödül.  
**Eventler:** `puzzle_started`, `puzzle_solved`, `wrong_click`, `hint_requested`, `retry_attempt`.  
**Test:** Oyuncu puzzle yapmadan ana tohumu alabilmeli; puzzle oynandığında ek eventler üretilmeli.  
**Not:** Puzzle tamamlamak portal açmanın zorunlu koşulu olmamalı.

### 1.14 Oyuncu Kontrol ve Kamera İyileştirme

**Durum:** Yapılacak  
**Amaç:** Hareketi ve kamera takibini daha akıcı, mobil oyuna daha yakın hâle getirmek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Yumuşak karakter hareketi; kamera takibi; temel mobil kontrol yaklaşımı.  
**Eventler:** Yok.  
**Test:** Oyuncu bölgelerde rahat hareket etmeli, kamera hedefi kaybetmemeli.  
**Not:** Kontroller 5–7 yaş grubu için basit tutulmalı.

### 1.15 Çocuk Dostu Görsel Atmosfer İyileştirme

**Durum:** Yapılacak  
**Amaç:** Teknik test sahnesini sıcak ve çocuk dostu bir oyun alanına dönüştürmek.  
**Kapsam:** Yalnızca `game/Assets`.  
**Çıktı:** Yumuşak renkler; bölgesel dekorlar; ışık, ağaç, taş ve kristal primitive’leri; tutarlı atmosfer.  
**Eventler:** Yok.  
**Test:** Sahne teknik prototipten çok sade bir oyun alanı gibi görünmeli.  
**Not:** Performans ve okunabilirlik ayrıntılı görsellikten önceliklidir.

### 1.16 Android APK Hazırlığı

**Durum:** Daha Sonra  
**Amaç:** Unity projesinden Android test APK’sı almak.  
**Kapsam:** Unity build ayarları ve gerektiğinde `game/Assets`.  
**Çıktı:** Android test APK’sı ve cihaz bağlantı ayarları.  
**Eventler:** Mevcut oyun eventleri.  
**Test:** APK Android cihazda açılmalı ve backend’e erişebilmelidir.  
**Not:** Cihaz testinde `localhost` yerine yapılandırılabilir bilgisayar IP’si kullanılmalı.

## 2. Backend, Veritabanı ve Event Altyapısı

### 2.1 Event Filtreleme

**Durum:** Yapılacak  
**Amaç:** Eventleri çocuk, tür, bölge ve tarih aralığına göre sorgulamak.  
**Kapsam:** Yalnızca `backend`; response formatı ve mevcut filtresiz davranış korunur.  
**Çıktı:** `GET /api/events` için güvenli ve isteğe bağlı query filtreleri.  
**Eventler:** Yeni event üretmez; kayıtlı tüm türleri filtreler.  
**Test:** Filtreler tek tek/birlikte, geçersiz değerlerle ve filtresiz istekle test edilir.  
**Not:** SQL sorguları parametrik olmalı.

### 2.2 Demo Data Reset ve Seed Scripti

**Durum:** Yapılacak  
**Amaç:** `demo-child-001` verisini kontrollü sıfırlayıp tekrar üretmek.  
**Kapsam:** Yalnızca `backend`; production verisini etkileyen genel silme işlemi yapılmaz.  
**Çıktı:** Açıkça adlandırılmış reset/seed komutları ve dengeli demo event dizisi.  
**Eventler:** Ana puzzle, keşif, NPC/seçim ve ödül eventleri.  
**Test:** Seed iki kez çalıştırılır; kayıt sayıları, hata çıktısı ve skor endpointi doğrulanır.  
**Not:** İşlem sadece sabit demo child kimliğiyle sınırlandırılmalı.

## 3. Davranış Analitiği ve Skorlama

### 3.1 Yeni Eventleri Skora Dahil Etme

**Durum:** Yapılacak  
**Amaç:** Yeni oyun eventlerinin altı davranış skoruna etkisini tanımlamak.  
**Kapsam:** Yalnızca `backend/src/controllers` ve gerekiyorsa backend testleri; endpoint formatı korunur.  
**Çıktı:** Puzzle, NPC/seçim ve keşif eventleri için açık skor eşlemesi.  
**Eventler:** Yol haritasındaki ana eventler.  
**Test:** Her event için kontrollü veriyle etkilenen ve etkilenmeyen skorlar doğrulanır.  
**Not:** Aynı event aynı girdide deterministik sonuç vermeli.

### 3.2 Skor Formüllerini Güncelleme

**Durum:** Yapılacak  
**Amaç:** Altı davranış skorunu dengeli ve açıklanabilir hâle getirmek.  
**Kapsam:** Yalnızca backend skor mantığı ve testleri; veritabanı şeması değiştirilmez.  
**Çıktı:** Belgelenmiş ağırlıklar, alt/üst sınırlar ve örnek senaryo sonuçları.  
**Eventler:** Skora dahil edilen tüm eventler.  
**Test:** Düşük, orta ve yüksek aktivite veri setlerinde altı skor ve sınırlar kontrol edilir.  
**Not:** `GET /api/scores/:childId` response alanları korunmalı.

## 4. Veli Paneli ve Raporlama

### 4.1 Event History Table

**Durum:** Yapılacak  
**Amaç:** Çocuğun oyun eventlerini anlaşılır ve filtrelenebilir bir geçmiş tablosunda göstermek.  
**Kapsam:** Yalnızca `parent-panel`; backend değişikliği gerekiyorsa önce 2.1 tamamlanır.  
**Çıktı:** Tarih, event türü, bölge, puzzle türü ve değer sütunlarıyla responsive tablo.  
**Eventler:** Yeni event üretmez; API eventlerini gösterir.  
**Test:** Dolu, boş, filtrelenmiş ve API hata durumları masaüstü/mobil görünümde kontrol edilir.  
**Not:** Mevcut özet kartları ve skor görünümü bozulmamalı.

### 4.2 Veli Öneri Metinleri

**Durum:** Yapılacak  
**Amaç:** Altı skora göre kısa, tarafsız ve eyleme dönük veli önerileri göstermek.  
**Kapsam:** Yalnızca `parent-panel`; tanı veya klinik değerlendirme dili kullanılmaz.  
**Çıktı:** Düşük/orta/yüksek skor aralıklarına bağlı öneri kartları ve veri yok durumu.  
**Eventler:** Yeni event üretmez; skor endpointini kullanır.  
**Test:** Örnek skor setlerinde doğru öneri, sınır değerleri ve boş veri davranışı doğrulanır.  
**Not:** Metinler gözlemsel bilgi olarak sunulmalı.

## 5. Demo, Test ve Dokümantasyon

### 5.1 README Demo Flow

**Durum:** Yapılacak  
**Amaç:** Projenin kurulumunu ve uçtan uca oyun/demo akışını tek belgede anlatmak.  
**Kapsam:** Kök `README.md`; uygulama kodları değiştirilmez.  
**Çıktı:** Gereksinimler, PostgreSQL/backend/panel/Unity çalıştırma ve oyun akışı.  
**Eventler:** Demo boyunca beklenen ana eventler belgelenir.  
**Test:** Adımlar temiz bir kontrol listesi olarak uygulanır; komutlar ve adresler doğrulanır.  
**Not:** Gizli bilgiler ve gerçek şifreler belgeye eklenmez.

### 5.2 Teknik Demo Senaryosu

**Durum:** Yapılacak  
**Amaç:** Jüri sunumunda tekrarlanabilir kısa bir teknik gösterim akışı hazırlamak.  
**Kapsam:** Yalnızca `docs`; oyun, backend ve panel kodları değiştirilmez.  
**Çıktı:** Hazırlık; 5–10 dakikalık gösterim sırası; beklenen sonuçlar; yedek plan.  
**Eventler:** Bölge, tohum, portal, puzzle ve NPC eventleri.  
**Test:** Senaryo baştan sona süre tutularak en az iki kez prova edilir.  
**Not:** Seed/reset adımı 2.2 ile uyumlu olmalı.

### 5.3 Final Rapor Güncelleme Notları

**Durum:** Yapılacak  
**Amaç:** Tamamlanan teknik çalışmaların bitirme raporuna aktarılacak özetini hazırlamak.  
**Kapsam:** Yalnızca `docs`; mevcut uygulama kodları değiştirilmez.  
**Çıktı:** Mimari, oyun ilerleyişi, event modeli, skor yaklaşımı, test sonuçları ve sınırlılıklar.  
**Eventler:** Ana event kataloğu ve skor ilişkileri belgelenir.  
**Test:** Notlar güncel oyun, endpoint, ekran ve demo akışıyla karşılaştırılır.  
**Not:** Akademik yorumlar doğrulanabilir proje çıktılarından ayrılmalı.

## Daha Sonra / Opsiyonel

- Tam açık dünya sistemi
- Store yayını
- Online deployment
- Gelişmiş AI öneri motoru
- JWT login sistemi
- Gerçek çocuk testi
- Tam pedagogik değerlendirme sistemi

Sıradaki Önerilen Görev: 1.4 Ana Hikâye Giriş Sahnesi
