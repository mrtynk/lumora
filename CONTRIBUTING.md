# Lumora'ya Katkı Rehberi

Bu rehber, Lumora üzerinde yapılan geliştirmelerin küçük, izlenebilir ve test edilebilir görevler hâlinde ilerlemesini sağlar.

## 1. Issue açma

Her geliştirme veya hata düzeltmesi için çalışmaya başlamadan önce bir GitHub issue açın.

- Yeni özelliklerde **Özellik / Geliştirme Görevi** şablonunu kullanın.
- Hatalarda **Hata Bildirimi** şablonunu kullanın.
- Kapsamı, dokunulmaması gereken alanları, test adımlarını ve kabul kriterlerini doldurun.
- Bir issue yalnızca tek, bağımsız ve doğrulanabilir işi kapsamalıdır.

## 2. Branch oluşturma

Güncel ana branch üzerinden issue'ya özel bir branch oluşturun:

```bash
git switch main
git pull
git switch -c feature/3-puzzle-popup-ui
```

Branch adı küçük harfli, kısa ve tire ile ayrılmış olmalıdır. Örnekler:

- `feature/2-unity-event-sender`
- `feature/7-isikli-vadi-flow`
- `fix/15-event-validation`
- `docs/13-readme`
- `refactor/10-score-rules`

## 3. Geliştirme yapma

- Yalnızca issue kapsamındaki dosya ve klasörlerde çalışın.
- Mevcut backend endpoint response formatlarını koruyun.
- Gereksiz dependency eklemeyin.
- Kodları sade, anlaşılır ve proje yapısıyla uyumlu tutun.
- Kapsam dışı bir ihtiyaç fark edilirse aynı branch'e eklemek yerine yeni issue açın.

## 4. Test etme

Issue'daki test adımlarını uygulayın. Değişikliğin ilgili katmanda çalıştığını ve mevcut özellikleri bozmadığını doğrulayın. Manuel testlerde kullanılan girdiyle gözlenen sonucu PR açıklamasına yazın.

## 5. Commit alma

Birbiriyle ilişkili değişiklikleri küçük ve açıklayıcı commit'lerde toplayın:

```bash
git add <ilgili-dosyalar>
git commit -m "feat(game): add puzzle event trigger"
```

Commit mesajı örnekleri:

- `feat(game): add hidden object prototype`
- `feat(backend): filter events by child id`
- `fix(panel): handle empty score response`
- `docs: add demo setup instructions`
- `test(backend): cover score calculation rules`

## 6. Push yapma

Branch'i uzak repoya gönderin:

```bash
git push -u origin feature/3-puzzle-popup-ui
```

## 7. Pull Request açma

- PR'ı ana branch'e hedefleyin ve PR şablonunu eksiksiz doldurun.
- İlgili issue'yu `Closes #<issue-numarası>` ile bağlayın.
- Yapılan değişiklikleri ve test sonucunu açıkça yazın.
- PR kapsamına ilgisiz dosyalar eklemeyin.
- İnceleme geri bildirimlerinden sonra testleri yeniden çalıştırın.

## Codex/AI ile çalışma kuralı

Codex veya başka bir AI aracıyla her seferinde yalnızca **tek issue** üzerinde ilerleyin. İstekte issue açıklamasını, izin verilen klasörleri, dokunulmaması gereken alanları ve kabul kriterlerini paylaşın. Araçtan önce mevcut yapıyı incelemesini, yalnızca verilen kapsamda değişiklik yapmasını ve görev sonunda test adımlarını açıklamasını isteyin. Birden fazla issue'yu aynı istemde birleştirmeyin.
