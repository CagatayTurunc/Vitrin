#!/bin/bash
# ============================================================
# Madde 20 — Veritabanı Geri Yükleme Scripti
# ============================================================
#
# Neden gerekli?
# Backup'lar sadece alınırsa yeterli değil — geri yüklenebilir
# olduklarını test etmek kritik. Bu script hem test hem de
# gerçek felaket kurtarma (disaster recovery) için kullanılır.
#
# Kullanım:
#   ./restore-postgres.sh /path/to/backup/vitrin_auth_20240827_020000.sql.gz
#   ./restore-postgres.sh /path/to/backup/vitrin_product_20240827_020000.sql.gz
#
# Veya S3'ten çek ve geri yükle:
#   S3_BACKUP_KEY="postgres/20240827_020000/vitrin_auth_20240827_020000.sql.gz" \
#   ./restore-postgres.sh
#
# Gereksinimler:
#   - POSTGRES_PASSWORD env değişkeni tanımlı olmalı (.env'den okunur)
#   - Docker ve psql kurulu olmalı
#   - (Opsiyonel) AWS CLI S3'ten otomatik çekmek için
#
# GÜVENLİK UYARISI:
#   Bu script mevcut veritabanını TAMAMEN SİLER ve yerine
#   backup'ı yükler. Production'da çalıştırmadan önce
#   mutlaka mevcut DB'nin yedeğini alın!
# ============================================================

set -euo pipefail

# ── Renk kodları (terminal output için) ─────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color
# ────────────────────────────────────────────────────────────

# ── Ayarlar ─────────────────────────────────────────────────
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-vitrin-postgres}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
BACKUP_FILE="${1:-}"
S3_BUCKET="${S3_BUCKET:-}"
S3_BACKUP_KEY="${S3_BACKUP_KEY:-}"

# .env dosyasından ayarları yükle (varsa)
if [ -f .env ]; then
    echo "→ .env dosyasından ayarlar yükleniyor..."
    export $(grep -v '^#' .env | xargs)
fi

# ────────────────────────────────────────────────────────────

echo "============================================================"
echo "  Vitrin PostgreSQL Restore Script"
echo "  $(date)"
echo "============================================================"
echo ""

# ── PARAMETRE KONTROLÜ ──────────────────────────────────────
if [ -z "$BACKUP_FILE" ] && [ -z "$S3_BACKUP_KEY" ]; then
    echo -e "${RED}❌ HATA: Backup dosyası belirtilmedi${NC}"
    echo ""
    echo "Kullanım:"
    echo "  $0 /path/to/backup.sql.gz"
    echo ""
    echo "Veya S3'ten çekmek için:"
    echo "  S3_BACKUP_KEY=\"postgres/20240827/vitrin_auth_20240827.sql.gz\" $0"
    echo ""
    exit 1
fi

# ── S3'TEN BACKUP ÇEK (opsiyonel) ───────────────────────────
if [ -n "$S3_BACKUP_KEY" ]; then
    if [ -z "$S3_BUCKET" ]; then
        echo -e "${RED}❌ HATA: S3_BUCKET tanımlı değil${NC}"
        exit 1
    fi
    
    echo "→ S3'ten backup çekiliyor: s3://$S3_BUCKET/$S3_BACKUP_KEY"
    BACKUP_FILE="/tmp/$(basename "$S3_BACKUP_KEY")"
    aws s3 cp "s3://$S3_BUCKET/$S3_BACKUP_KEY" "$BACKUP_FILE"
    echo -e "   ${GREEN}✅ Backup indirildi: $BACKUP_FILE${NC}"
fi

# ── BACKUP DOSYASI KONTROLÜ ─────────────────────────────────
if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}❌ HATA: Backup dosyası bulunamadı: $BACKUP_FILE${NC}"
    exit 1
fi

echo -e "${GREEN}→ Backup dosyası bulundu: $BACKUP_FILE${NC}"

# Dosya adından veritabanı ismini çıkar
# Örnek: vitrin_auth_20240827_020000.sql.gz → vitrin_auth
DB_NAME=$(basename "$BACKUP_FILE" | sed -E 's/^(vitrin_[a-z]+)_.*/\1/')

if [[ ! "$DB_NAME" =~ ^vitrin_(auth|product|comment|voting|notification|analytics|ai)$ ]]; then
    echo -e "${RED}❌ HATA: Dosya adından geçerli veritabanı ismi çıkarılamadı: $DB_NAME${NC}"
    echo "Desteklenen: vitrin_auth, vitrin_product, vitrin_comment, vitrin_voting, vitrin_notification, vitrin_analytics, vitrin_ai"
    exit 1
fi

echo -e "${YELLOW}→ Hedef veritabanı: $DB_NAME${NC}"
echo ""

# ── GÜVENLİK ONAYI ──────────────────────────────────────────
echo -e "${RED}⚠️  UYARI: Bu işlem mevcut '$DB_NAME' veritabanını TAMAMEN SİLER!${NC}"
echo "Devam etmeden önce mevcut veritabanının yedeğini aldığınızdan emin olun."
echo ""
read -p "Devam etmek istiyor musunuz? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "İşlem iptal edildi."
    exit 0
fi

echo ""
echo "============================================================"
echo "  RESTORE İŞLEMİ BAŞLIYOR"
echo "============================================================"

# ── CONTAINER KONTROLÜ ──────────────────────────────────────
if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
    echo -e "${RED}❌ HATA: PostgreSQL container çalışmıyor: $POSTGRES_CONTAINER${NC}"
    echo "Container'ı başlatın: docker compose up -d postgres"
    exit 1
fi

echo -e "${GREEN}✅ PostgreSQL container çalışıyor${NC}"

# ── MEVCUT BAĞLANTILARI KAPAT ──────────────────────────────
echo "→ Mevcut bağlantılar kapatılıyor..."
docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c \
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid();" \
    2>/dev/null || true
echo -e "   ${GREEN}✅ Bağlantılar kapatıldı${NC}"

# ── VERİTABANINI SİL VE YENİDEN OLUŞTUR ────────────────────
echo "→ Veritabanı siliniyor ve yeniden oluşturuluyor..."
docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c \
    "DROP DATABASE IF EXISTS $DB_NAME;" \
    2>/dev/null || true

docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c \
    "CREATE DATABASE $DB_NAME OWNER $POSTGRES_USER;"

echo -e "   ${GREEN}✅ Veritabanı yeniden oluşturuldu${NC}"

# ── BACKUP'I GERİ YÜKLE ─────────────────────────────────────
echo "→ Backup geri yükleniyor..."
echo "   Dosya: $BACKUP_FILE"
echo "   Hedef: $DB_NAME"
echo ""

# Backup sıkıştırılmış mı kontrol et
if [[ "$BACKUP_FILE" == *.gz ]]; then
    echo "   (gzip ile sıkıştırılmış backup açılıyor...)"
    gunzip -c "$BACKUP_FILE" | docker exec -i "$POSTGRES_CONTAINER" \
        psql -U "$POSTGRES_USER" -d "$DB_NAME" \
        2>&1 | grep -E "(ERROR|FATAL)" || true
else
    echo "   (sıkıştırılmamış backup yükleniyor...)"
    docker exec -i "$POSTGRES_CONTAINER" \
        psql -U "$POSTGRES_USER" -d "$DB_NAME" < "$BACKUP_FILE" \
        2>&1 | grep -E "(ERROR|FATAL)" || true
fi

# ── RESTORE DOĞRULAMA ───────────────────────────────────────
echo ""
echo "→ Restore doğrulanıyor..."

# Tablo sayısını kontrol et
TABLE_COUNT=$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$DB_NAME" -t -c \
    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" | xargs)

echo "   Tablo sayısı: $TABLE_COUNT"

if [ "$TABLE_COUNT" -eq 0 ]; then
    echo -e "${RED}❌ UYARI: Veritabanında hiç tablo yok — restore başarısız olmuş olabilir${NC}"
    exit 1
fi

# Örnek bir satır sayımı (ilk tablodan)
FIRST_TABLE=$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$DB_NAME" -t -c \
    "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' LIMIT 1;" | xargs)

if [ -n "$FIRST_TABLE" ]; then
    ROW_COUNT=$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$DB_NAME" -t -c \
        "SELECT COUNT(*) FROM \"$FIRST_TABLE\";" 2>/dev/null | xargs || echo "0")
    echo "   Örnek tablo '$FIRST_TABLE' satır sayısı: $ROW_COUNT"
fi

echo ""
echo "============================================================"
echo -e "  ${GREEN}✅ RESTORE TAMAMLANDI${NC}"
echo "============================================================"
echo ""
echo "Veritabanı: $DB_NAME"
echo "Backup: $BACKUP_FILE"
echo "Tablo sayısı: $TABLE_COUNT"
echo ""
echo "Sonraki adımlar:"
echo "  1. Uygulamayı test edin (login, CRUD işlemleri)"
echo "  2. Migration'ların çalıştığını kontrol edin"
echo "  3. Veri bütünlüğünü doğrulayın (foreign key'ler, constraint'ler)"
echo ""
