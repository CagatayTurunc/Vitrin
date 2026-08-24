# CI/CD Pipeline Kurulum Rehberi

## Gerekli GitHub Secrets

GitHub repo → Settings → Secrets and variables → Actions → New repository secret

| Secret Adı | Açıklama | Nereden Alınır |
|---|---|---|
| `EC2_HOST` | EC2 instance public IP | AWS Console → EC2 |
| `EC2_USER` | SSH kullanıcı adı (genellikle `ubuntu`) | EC2 AMI'ye göre değişir |
| `EC2_SSH_KEY` | EC2 private key (.pem içeriği) | AWS Key Pairs → .pem dosyası |
| `DISCORD_WEBHOOK_URL` | Discord webhook URL'i | Aşağıdaki adımlara bak |
| `E2E_TEST_EMAIL` | Smoke test kullanıcısı e-posta | Test ortamında oluştur |
| `E2E_TEST_PASSWORD` | Smoke test kullanıcısı şifre | Test ortamında oluştur |

---

## Discord Webhook Kurulumu

1. Discord'da bildirim almak istediğin kanalı aç
2. Kanal adına sağ tıkla → **Kanal Düzenle**
3. Sol menüden **Entegrasyonlar** → **Webhook'lar**
4. **Yeni Webhook** → İsim: `Vitrin Deploy Bot`
5. **Webhook URL'sini Kopyala**
6. GitHub'a git: Settings → Secrets → `DISCORD_WEBHOOK_URL` olarak ekle

Bildirimler şu durumları rapor eder:
- ✅ Deploy başarılı
- ❌ Deploy başarısız
- 🔄 Otomatik rollback uygulandı
- ⚠️ Kısmi başarı (uyarılar var)

---

## Branch Protection Rules Kurulumu

### Otomatik (gh CLI ile)

```bash
# gh CLI kurulu değilse: https://cli.github.com
gh auth login
bash .github/branch-protection-setup.sh
```

### Manuel (GitHub UI üzerinden)

1. GitHub repo → **Settings** → **Branches**
2. **Add branch ruleset** → Branch name pattern: `main`
3. Şu kuralları aktif et:

| Kural | Ayar |
|---|---|
| Require a pull request before merging | ✅ Açık |
| Required approvals | 1 |
| Dismiss stale pull request approvals when new commits are pushed | ✅ Açık |
| Require status checks to pass before merging | ✅ Açık |
| Status checks (zorunlu) | `Backend Tests`, `Frontend Checks`, `Security Scan` |
| Require branches to be up to date before merging | ✅ Açık |
| Block force pushes | ✅ Açık |

---

## Geliştirme Akışı

```
feature/xyz  →  develop  →  main  →  production
    PR açılır     PR açılır    merge olur    deploy.yml tetiklenir
    CI çalışır    CI çalışır   1 onay gerekli
```

### Adım adım

```bash
# 1. develop'tan yeni feature branch aç
git checkout develop
git pull origin develop
git checkout -b feature/yeni-ozellik

# 2. Değişikliklerini yap
# ...

# 3. Commit ve push
git add .
git commit -m "feat: yeni özellik eklendi"
git push origin feature/yeni-ozellik

# 4. GitHub'da develop'a PR aç
# → CI otomatik çalışır (backend + frontend + security)
# → PR review sonrası merge

# 5. Yeterince özellik birikinceye veya release zamanı develop→main PR aç
# → CI çalışır + 1 onay gerekli
# → Merge sonrası deploy.yml otomatik tetiklenir
```

---

## Pipeline Akışı

```
push to main
    │
    ├─► Backend Tests ──────────────────────────────────────────────┐
    │                                                               │
    └─► Security Audit ─────────────────────────────────────────────┤
                                                                    │
                    Matrix: Build & Push 9 servis ◄─────────────────┘
                    (auth, product, voting, comment,
                     notification, analytics, ai, gateway, web)
                    Her servis: SHA tag + latest tag
                            │
                            ▼
                    Deploy to EC2
                    - Tüm 9 servis pull
                    - Rolling restart (sıralı)
                    - Her servis için health check
                            │
                            ▼
                    Smoke Tests (Production)
                    - Playwright E2E
                    - vitrin.it.com üzerinden
                            │
                    ┌───────┴────────┐
                    │               │
                   ✅              ❌
                    │               │
               Discord          Auto Rollback
               Bildirim         + Discord Bildirim
```
