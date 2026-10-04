# Lumora Proje Yol Haritası

> Codex’e görev verirken örnek: **“TASKS.md içindeki 1.4 görevini uygula, CODEX_PROMPT.md kurallarına uy.”**

Her görev ayrı branch/PR üzerinde ve yalnızca belirtilen kapsamda yapılır. Durum değerleri: `Yapılacak`, `Devam Ediyor`, `Tamamlandı`. Ortak kurallar için `CODEX_PROMPT.md` esas alınır.

## 1. Unity Oyun ve Etkileşim Prototipi

### 1.1 Prototype Scene Builder

**Durum:** Tamamlandı  
**Amaç:** Unity’de demo sahnesini otomatik kuran `Tools > Lumora > Build Prototype Scene` sistemini oluşturmak.  
**Kapsam:** `game/Assets/Editor`, `game/Assets/Scripts`, `game/Assets/Scenes`, `game/Assets/Materials`.  
**Çıktı:** Ground, Player, GameManager, PuzzlePaper, Main Camera ve Directional Light içeren `PrototypeScene`.  
**Eventler:** Yok.  
**Test:** Menü çalıştırılır; sahne objeleri ve oyuncu hareketi Play Mode’da doğrulanır.  
**Not:** Tamamlandı.

### 1.2 Unity Backend Event Sender

**Durum:** Tamamlandı  
**Amaç:** Unity’den backend `POST /api/events` endpointine event göndermek.  
**Kapsam:** `game/Assets/Scripts`, `game/Assets/Editor`.  
**Çıktı:** `GameEventSender.cs` ile JSON event gönderimi ve sahne bağlantısı.  
**Eventler:** `puzzle_started`.  
**Test:** Backend açıkken Unity etkileşimi yapılır; kayıt `GET /api/events` ile kontrol edilir.  
**Not:** Tamamlandı.

### 1.3 Puzzle Popup UI

**Durum:** Tamamlandı  
**Amaç:** PuzzlePaper yanında E’ye basınca popup açmak ve popup sonuçlarını backend’e göndermek.  
**Kapsam:** `game/Assets/Scripts`, `game/Assets/Editor`, `game/Assets/UI`, `game/Assets/Scenes`.  
**Çıktı:** `PuzzlePopupUI.cs`, güncellenmiş `PuzzleTrigger.cs` ve otomatik UI kuran sahne builder.  
**Eventler:** `puzzle_started`, `puzzle_solved`, `puzzle_failed`, `puzzle_abandoned`.  
**Test:** Popup açılır; üç sonuç butonu ayrı ayrı denenip API kayıtları doğrulanır.  
**Not:** Tamamlandı.

### 1.4 Hidden Object Demo Puzzle

**Durum:** Yapılacak  
**Amaç:** Çocuğun doğru nesneyi bularak hidden object mini puzzle’ını tamamlamasını sağlamak.  
**Kapsam:** Yalnızca `game/Assets`; backend ve parent panel değiştirilmez.  
**Çıktı:** Üç tıklanabilir obje, doğru “Işık Tohumu”, iki yanlış obje ve ipucu butonu.  
**Eventler:** `puzzle_started`, `wrong_click`, `hint_requested`, `puzzle_solved`, `puzzle_abandoned`.  
**Test:** Backend açıkken puzzle açılır; yanlış obje, ipucu, doğru obje ve kapatma akışları `GET /api/events` üzerinden doğrulanır.  
**Not:** Mevcut popup ve otomatik sahne builder genişletilmeli; Inspector’da uzun manuel bağlantı gerektirmemeli.

### 1.5 Memory Match Demo Puzzle

**Durum:** Yapılacak  
**Amaç:** Üç kart çiftiyle tekrar oynanabilir hafıza eşleştirme puzzle’ı oluşturmak.  
**Kapsam:** Yalnızca `game/Assets`; mevcut hidden object akışı bozulmaz.  
**Çıktı:** Karıştırılan kartlar, eşleşme kontrolü, deneme sayacı ve tamamlanma durumu.  
**Eventler:** `puzzle_started`, `retry_attempt`, `hint_requested`, `puzzle_solved`, `puzzle_abandoned`.  
**Test:** Doğru/yanlış eşleşmeler, ipucu, bitiş ve yeniden açma Play Mode’da ve API’de kontrol edilir.  
**Not:** Ortak popup/event bileşenleri yeniden kullanılmalı.

### 1.6 Pattern Puzzle Demo

**Durum:** Yapılacak  
**Amaç:** Basit renk veya şekil örüntüsünü tamamlatan mini puzzle geliştirmek.  
**Kapsam:** Yalnızca `game/Assets`; diğer puzzle türleri değiştirilmez.  
**Çıktı:** En az üç örüntü, cevap seçenekleri, geri bildirim ve tekrar deneme akışı.  
**Eventler:** `puzzle_started`, `wrong_click`, `retry_attempt`, `hint_requested`, `puzzle_solved`, `puzzle_abandoned`.  
**Test:** Yanlış/doğru cevap, ipucu, tekrar ve kapatma sonuçları Play Mode’da ve API’de doğrulanır.  
**Not:** Puzzle türü eventlerde `pattern` olarak ayırt edilebilir olmalı.

### 1.7 NPC ve Seçim Etkileşimi

**Durum:** Yapılacak  
**Amaç:** Oyuncunun örnek bir NPC ile konuşup iki seçenekten birini seçmesini sağlamak.  
**Kapsam:** `game/Assets/Scripts`, `game/Assets/Editor`, `game/Assets/UI`, `game/Assets/Scenes`.  
**Çıktı:** Yaklaşma tetikleyicisi, kısa diyalog UI’ı, iki seçim ve yardım sonucu.  
**Eventler:** `dialogue_selected`, `choice_made`, `npc_helped`.  
**Test:** Her seçim ayrı oynanır; UI akışı ve gönderilen event değerleri API’den kontrol edilir.  
**Not:** Sahne bağlantıları mümkün olduğunca builder tarafından kurulmalı.

### 1.8 Işıklı Vadi Demo Akışı

**Durum:** Yapılacak  
**Amaç:** Keşif, puzzle, NPC ve ödül adımlarını tek oynanabilir demo akışında birleştirmek.  
**Kapsam:** Yalnızca `game/Assets`; backend ve parent panel değiştirilmez.  
**Çıktı:** Başlangıçtan ödüle kadar sıralı, bloklanmayan Işıklı Vadi demo akışı.  
**Eventler:** `area_explored`, puzzle eventleri, `dialogue_selected`, `choice_made`, `npc_helped`, `reward_collected`.  
**Test:** Temiz sahnede demo baştan sona oynanır; sıra, tekrar açma ve tüm API kayıtları doğrulanır.  
**Not:** Tek bölge ve kısa teslim demosu kapsamıyla sınırlı kalmalı.

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
**Çıktı:** `wrong_click`, `hint_requested`, `retry_attempt`, NPC/seçim ve keşif eventleri için açık skor eşlemesi.  
**Eventler:** Yol haritasındaki 12 ana event.  
**Test:** Her event için kontrollü veriyle etkilenen ve etkilenmeyen skorlar doğrulanır.  
**Not:** Aynı event aynı girdide deterministik sonuç vermeli.

### 3.2 Skor Formüllerini Güncelleme

**Durum:** Yapılacak  
**Amaç:** Dikkat, Azim, Merak, Bağımsızlık, Stratejik Düşünme ve Sosyal Eğilim skorlarını dengeli hâle getirmek.  
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
**Amaç:** Projenin kurulumunu ve uçtan uca demo akışını tek belgede anlatmak.  
**Kapsam:** Kök `README.md`; uygulama kodları değiştirilmez.  
**Çıktı:** Gereksinimler, PostgreSQL/backend/panel/Unity çalıştırma ve demo adımları.  
**Eventler:** Demo boyunca beklenen ana eventler belgelenir.  
**Test:** Adımlar temiz bir kontrol listesi olarak uygulanır; komutlar ve adresler doğrulanır.  
**Not:** Gizli bilgiler ve gerçek şifreler belgeye eklenmez.

### 5.2 Teknik Demo Senaryosu

**Durum:** Yapılacak  
**Amaç:** Jüri sunumunda tekrarlanabilir kısa bir teknik gösterim akışı hazırlamak.  
**Kapsam:** `docs`; oyun, backend ve panel kodları değiştirilmez.  
**Çıktı:** Hazırlık, 5–10 dakikalık gösterim sırası, beklenen sonuçlar ve hata durumunda yedek plan.  
**Eventler:** Puzzle, keşif, NPC/seçim ve ödül eventleri.  
**Test:** Senaryo baştan sona süre tutularak en az iki kez prova edilir.  
**Not:** Seed/reset adımı 2.2 ile uyumlu olmalı.

### 5.3 Android APK Hazırlığı

**Durum:** Yapılacak  
**Amaç:** Işıklı Vadi demosunu Android cihazda kurulabilir ve test edilebilir hâle getirmek.  
**Kapsam:** `game/Assets`, `game/ProjectSettings` ve Android build ayarları; backend değiştirilmez.  
**Çıktı:** Geliştirme APK’sı, cihazdan erişilebilir backend adresi ve build notları.  
**Eventler:** Unity demosunun ürettiği mevcut eventler.  
**Test:** APK gerçek cihazda kurulur; hareket, popup, puzzle ve ağ istekleri aynı Wi-Fi üzerinde denenir.  
**Not:** `localhost` yerine yapılandırılabilir bilgisayar IP’si kullanılmalı.

### 5.4 Final Rapor Güncelleme Notları

**Durum:** Yapılacak  
**Amaç:** Tamamlanan teknik çalışmaların bitirme raporuna aktarılacak özetini hazırlamak.  
**Kapsam:** Yalnızca `docs`; mevcut uygulama kodları değiştirilmez.  
**Çıktı:** Mimari, event modeli, skor yaklaşımı, test sonuçları, ekran görüntüsü listesi ve sınırlılıklar için güncelleme notları.  
**Eventler:** Ana event kataloğu ve skor ilişkileri belgelenir.  
**Test:** Notlar güncel endpoint, ekran ve demo akışıyla karşılaştırılır.  
**Not:** Akademik yorumlar doğrulanabilir proje çıktılarından ayrılmalı.

## Daha Sonra / Opsiyonel

Ana teslim hedefi tamamlandıktan sonra ayrı kapsamlandırılabilir:

- Tam açık dünya sistemi
- Dört bölgenin tamamını eksiksiz geliştirme
- Store yayını
- Online deployment
- Gelişmiş AI öneri motoru
- JWT login sistemi
- Gerçek çocuk testi
- Tam pedagogik değerlendirme sistemi

Sıradaki Önerilen Görev: 1.4 Hidden Object Demo Puzzle
