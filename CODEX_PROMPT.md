# Lumora — Codex Görev Bağlamı

Bu belge, Lumora üzerinde çalışan Codex/AI aracına her issue ile birlikte verilecek genel proje bağlamıdır. Göreve özel issue açıklaması ve kabul kriterleri ayrıca paylaşılmalıdır.

## Proje adı

**Lumora — Mobil Oyun Aracılığıyla Çocuk Davranış Analitiği ve Ebeveyn Analiz Arayüzü Geliştirme**

## Proje amacı

Lumora, 5–7 yaş arası çocuklara yönelik hikâye tabanlı bir 3D mobil macera oyunudur. Çocuğun puzzle çözme, ipucu kullanma, keşif, NPC etkileşimi ve seçim davranışları event olarak backend'e gönderilir. Eventler PostgreSQL'e kaydedilir, kural tabanlı analitik motor tarafından altı beceri skoruna dönüştürülür ve React tabanlı veli panelinde gösterilir.

## Kullanılan teknolojiler

- Oyun: Unity ve C#
- Backend: Node.js ve Express
- Veritabanı: PostgreSQL (`lumora_db`)
- Veli paneli: React ve Vite

## Proje klasör yapısı

```text
Lumora/
├── backend/       # Express API ve analitik mantığı
├── parent-panel/  # React tabanlı veli arayüzü
├── game/          # Unity mobil oyun projesi
└── docs/          # Proje belgeleri
```

## Backend endpointleri

- `GET /`
- `GET /api/db-test`
- `POST /api/events`
- `GET /api/events`
- `GET /api/scores/:childId`

## Temel eventler

- `puzzle_started`
- `puzzle_solved`
- `hint_requested`
- `retry_attempt`
- `area_explored`
- `npc_helped`
- `choice_made`
- `wrong_click`
- `puzzle_failed`
- `puzzle_abandoned`
- `boss_attempt`
- `reward_collected`
- `dialogue_selected`
- `save_game`

## Altı beceri skoru

- Dikkat
- Azim
- Merak
- Bağımsızlık
- Stratejik düşünme
- Sosyal eğilim

## Genel kurallar

- Mevcut backend yapısını bozma.
- Mevcut parent-panel yapısını bozma.
- Sadece görevde belirtilen klasörlerde çalış.
- Gereksiz dependency ekleme.
- Kodları sade ve anlaşılır tut.
- Her görev sonunda nasıl test edileceğini açıkla.
- Unity tarafında mümkünse otomatik sahne kurulum scriptleri kullan.
- Backend endpoint response formatlarını bozma.
- Her seferinde yalnızca tek issue üzerinde çalış.
- Kapsam dışı değişiklik yapma; ek ihtiyaçları ayrı issue olarak öner.
- Göreve başlamadan önce ilgili mevcut dosyaları incele.
- Kullanıcının mevcut ve ilgisiz değişikliklerini koru.

## Görev istemi için önerilen ek bilgiler

Her görevde bu belgeye ek olarak issue numarasını, görev açıklamasını, kapsamı, dokunulmaması gereken yerleri, beklenen çıktıyı, test adımlarını ve kabul kriterlerini paylaş.
