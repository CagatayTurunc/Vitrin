# EC2 Cache & Resource Management

## Sorun

EC2 instance'a SSH bağlantısı zaman zaman tamamen kopuyor, reboot olmadan düzelmiyordu.

**Kök neden:** `docker build` komutlarının bıraktığı build cache zamanla 19GB+ boyutuna ulaşıyor, 30GB'lık diski %96'ya dolduruyor. Disk dolunca SSH daemon yeni bağlantı kuramıyor.

İkincil sorun: 3.7GB RAM'de swap tanımlı değildi. Bellek baskısı altında kernel SSH proseslerini öldürüyordu.

---

## Uygulanan Çözümler

### 1. Acil Disk Temizliği

```bash
docker system prune -af --volumes
# Sonuç: 19.65GB açıldı, %96 → %31
```

> `--volumes` flag'i kullanılmayan volume'ları da siler. Çalışan container'lar etkilenmez.

### 2. Swap Alanı Ekleme

```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# Reboot'ta da aktif olması için fstab'a ekle
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

**Sonuç:** Swap: 0 → 2GB. Bellek baskısında kernel artık swap kullanır, SSH proseslerini öldürmez.

### 3. Otomatik Cache Temizliği (Cron)

Amazon Linux 2023'te cron varsayılan olarak gelmiyor, önce kur:

```bash
sudo dnf install -y cronie
sudo systemctl enable --now crond
```

Her gece 03:00'da 24 saatten eski build cache'i temizleyen job:

```bash
(crontab -l 2>/dev/null; echo "0 3 * * * /usr/bin/docker system prune -f --filter 'until=24h' 2>&1 | /usr/bin/logger -t docker-prune") | crontab -
```

Kontrol:

```bash
crontab -l
# 0 3 * * * /usr/bin/docker system prune -f --filter 'until=24h' ...
```

Log'ları görmek için:

```bash
sudo journalctl -t docker-prune
```

---

## Güncel Durum

| Metrik | Öncesi | Sonrası |
|--------|--------|---------|
| Disk kullanımı | %96 (1.4GB boş) | %31 (21GB boş) |
| Swap | 0 | 2GB |
| Otomatik temizlik | Yok | Her gece 03:00 |

---

## Manuel Kontrol Komutları

```bash
# Disk durumu
df -h /

# Memory + swap
free -h

# Docker ne kadar yer kaplıyor
docker system df

# Detaylı cache bilgisi
docker buildx du 2>/dev/null || docker system df -v
```

---

## Önleyici Notlar

- `docker build` sık çalıştırılıyorsa `--no-cache` flag'ini kritik olmayan build'lerde kullan
- CI/CD pipeline'ında build sonrası `docker system prune -f` eklenebilir
- Disk %80'i geçerse alarm kurmak için CloudWatch Agent kullanılabilir
- Instance tipi upgrade'i gerekirse: t3.small → t3.medium (RAM 2GB → 4GB)
