#!/bin/bash
# ============================================================
# Madde 20 — Otomatik Yedek: PostgreSQL Backup Script
# ============================================================
#
# Neden gerekli?
# Docker volume'lar sunucu çöktüğünde veya yanlışlıkla silindiğinde
# veri kaybına yol açar. Offsite backup bu riski ortadan kaldırır.
#
# Kullanım:
#   ./backup-postgres.sh
#
# Cron ile otomatik (her gün gece 02:00):
#   0 2 * * * /path/to/vitrin/scripts/backup-postgres.sh >> /var/log/vitrin-backup.log 2>&1
#
# Gereksinimler:
#   - POSTGRES_PASSWORD env değişkeni tanımlı olmalı
#   - Docker ve pg_dump kurulu olmalı
#   - (Opsiyonel) AWS CLI veya rclone S3 upload için
# ============================================================

set -euo pipefail

# ── Ayarlar ─────────────────────────────────────────────────
BACKUP_DIR="${BACKUP_DIR:-/var/backups/vitrin-postgres}"
RETENTION_DAYS="${RETENTION_DAYS:-7}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-vitrin-postgres}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Yedeklenecek veritabanları
DATABASES=("vitrin_auth" "vitrin_product" "vitrin_comment")

# S3 upload (opsiyonel — S3_BUCKET tanımlanmışsa aktif)
S3_BUCKET="${S3_BUCKET:-}"
# ────────────────────────────────────────────────────────────

echo "=== Vitrin PostgreSQL Backup — $(date) ==="

# Backup dizini oluştur
mkdir -p "$BACKUP_DIR"

# Her veritabanı için ayrı dump al
for DB in "${DATABASES[@]}"; do
    BACKUP_FILE="$BACKUP_DIR/${DB}_${TIMESTAMP}.sql.gz"
    echo "→ Yedekleniyor: $DB → $BACKUP_FILE"

    docker exec "$POSTGRES_CONTAINER" \
        pg_dump -U "$POSTGRES_USER" "$DB" \
        | gzip > "$BACKUP_FILE"

    # Dosya boyutunu kontrol et — boş dump alarm ver
    FILESIZE=$(stat -f%z "$BACKUP_FILE" 2>/dev/null || stat -c%s "$BACKUP_FILE")
    if [ "$FILESIZE" -lt 100 ]; then
        echo "⚠️  UYARI: $DB yedeği çok küçük ($FILESIZE byte) — kontrol edin!"
    else
        echo "   ✅ $DB yedeklendi ($FILESIZE byte)"
    fi
done

# Eski yedekleri temizle (RETENTION_DAYS gün öncesi)
echo "→ $RETENTION_DAYS günden eski yedekler temizleniyor..."
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +"$RETENTION_DAYS" -delete
echo "   ✅ Temizleme tamamlandı"

# S3'e yükle (opsiyonel)
if [ -n "$S3_BUCKET" ]; then
    echo "→ S3'e yükleniyor: s3://$S3_BUCKET/postgres/$TIMESTAMP/"
    aws s3 cp "$BACKUP_DIR/" "s3://$S3_BUCKET/postgres/$TIMESTAMP/" \
        --recursive \
        --exclude "*" \
        --include "*_${TIMESTAMP}.sql.gz" \
        --storage-class STANDARD_IA
    echo "   ✅ S3 yüklemesi tamamlandı"
fi

echo "=== Backup tamamlandı — $(date) ==="
