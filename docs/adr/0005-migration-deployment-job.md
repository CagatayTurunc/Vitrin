# ADR-0005: Migration'ları uygulama başlangıcından ayırma

- Durum: Kabul edildi
- Tarih: 2026-07-14

## Bağlam

Her API kendi `DbContext` migration'larını process başlangıcında çalıştırıyordu. Birden fazla replika aynı anda açıldığında migration kilidi, uzun startup süresi ve kısmi rollout riski oluşuyordu. Uygulamanın schema değiştirme yetkisine sürekli sahip olması da least-privilege yaklaşımıyla uyuşmuyordu.

## Karar

API process'leri normal başlangıçta migration çalıştırmayacaktır. Her servis imajı yalnızca `--migrate-only` argümanıyla başlatıldığında migration'ları uygular ve web sunucusunu açmadan başarı/hata koduyla kapanır.

Docker Compose için [run-migrations.ps1](../../scripts/run-migrations.ps1) tek-seferlik deployment job'ıdır. Üretim orkestratöründe aynı sözleşme Kubernetes Job, init pipeline adımı veya eşdeğer bir release job ile uygulanmalıdır. Job başarıyla bitmeden yeni uygulama sürümü rollout edilmemelidir.

## Sonuçlar

- Aynı schema için migration yapan tek kontrollü süreç vardır.
- Migration ve uygulama rollout logları birbirinden ayrılır.
- Uygulama kimliği production'da DDL yetkisi olmadan çalıştırılabilir.
- Yeni/boş ortamda normal servis başlangıcından önce migration job'ını çalıştırmak zorunludur.
- Migration geriye uyumlu hazırlanmalı; destructive değişiklikler expand/contract ile en az iki release'e bölünmelidir.
