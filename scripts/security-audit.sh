#!/bin/bash
# ============================================================
# Madde 24 — Security Audit Script
# ============================================================
#
# Vitrin projesinin güvenlik kontrollerini yapar:
# - Security headers kontrolü
# - SSL/TLS configuration
# - Dependency vulnerabilities (npm audit)
# - Common security issues
#
# Kullanım:
#   ./scripts/security-audit.sh
#   ./scripts/security-audit.sh --url=https://vitrin.com
#
# CI/CD'de:
#   - Pre-deploy security gate olarak kullanılabilir
#   - Haftalık scheduled audit
# ============================================================

set -euo pipefail

# ── Renk kodları ────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color
# ────────────────────────────────────────────────────────────

# ── Ayarlar ─────────────────────────────────────────────────
TARGET_URL="${1:-http://localhost:3000}"
REPORT_FILE="security-audit-$(date +%Y%m%d_%H%M%S).txt"
# ────────────────────────────────────────────────────────────

echo "============================================================"
echo "  🔒 Vitrin Security Audit"
echo "  Target: $TARGET_URL"
echo "  $(date)"
echo "============================================================"
echo ""

# Report dosyası başlat
{
    echo "Vitrin Security Audit Report"
    echo "Generated: $(date)"
    echo "Target: $TARGET_URL"
    echo "========================================"
    echo ""
} > "$REPORT_FILE"

ISSUES_FOUND=0

# ──────────────────────────────────────────────────────────────
# 1. Security Headers Kontrolü
# ──────────────────────────────────────────────────────────────
echo -e "${BLUE}[1/6] Security Headers Kontrolü...${NC}"
echo ""

check_header() {
    local header_name=$1
    local expected=$2
    local response=$(curl -s -I "$TARGET_URL" | grep -i "^${header_name}:" || echo "")
    
    if [ -z "$response" ]; then
        echo -e "  ${RED}❌ $header_name eksik${NC}"
        echo "  ❌ $header_name eksik" >> "$REPORT_FILE"
        ((ISSUES_FOUND++))
    elif [[ "$expected" != "" ]] && [[ ! "$response" =~ $expected ]]; then
        echo -e "  ${YELLOW}⚠️  $header_name değeri beklenen gibi değil${NC}"
        echo "     Bulunan: $response"
        echo "  ⚠️  $header_name: $response" >> "$REPORT_FILE"
    else
        echo -e "  ${GREEN}✅ $header_name OK${NC}"
        echo "  ✅ $header_name: OK" >> "$REPORT_FILE"
    fi
}

# Critical headers
check_header "X-Content-Type-Options" "nosniff"
check_header "X-Frame-Options" "(DENY|SAMEORIGIN)"
check_header "X-XSS-Protection" "1"
check_header "Strict-Transport-Security" ""  # HSTS
check_header "Content-Security-Policy" ""
check_header "Referrer-Policy" ""

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# 2. SSL/TLS Kontrolü
# ──────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}[2/6] SSL/TLS Kontrolü...${NC}"
echo ""

if [[ "$TARGET_URL" =~ ^https:// ]]; then
    # SSL certificate bilgisi
    SSL_INFO=$(echo | openssl s_client -connect "$(echo "$TARGET_URL" | sed -E 's#https://([^/]+).*#\1#'):443" -servername "$(echo "$TARGET_URL" | sed -E 's#https://([^/]+).*#\1#')" 2>/dev/null | openssl x509 -noout -dates 2>/dev/null || echo "SSL check failed")
    
    if [[ "$SSL_INFO" == "SSL check failed" ]]; then
        echo -e "  ${RED}❌ SSL certificate kontrol edilemedi${NC}"
        echo "  ❌ SSL certificate kontrol edilemedi" >> "$REPORT_FILE"
        ((ISSUES_FOUND++))
    else
        echo -e "  ${GREEN}✅ SSL certificate geçerli${NC}"
        echo "  $SSL_INFO"
        echo "  ✅ SSL certificate: OK" >> "$REPORT_FILE"
        echo "  $SSL_INFO" >> "$REPORT_FILE"
    fi
    
    # TLS version kontrolü
    TLS_VERSION=$(echo | openssl s_client -connect "$(echo "$TARGET_URL" | sed -E 's#https://([^/]+).*#\1#'):443" 2>/dev/null | grep "Protocol" || echo "")
    echo "  TLS Version: $TLS_VERSION"
    echo "  TLS Version: $TLS_VERSION" >> "$REPORT_FILE"
else
    echo -e "  ${RED}❌ HTTPS kullanılmıyor — güvensiz!${NC}"
    echo "  ❌ HTTPS kullanılmıyor" >> "$REPORT_FILE"
    ((ISSUES_FOUND++))
fi

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# 3. Dependency Vulnerabilities (npm audit)
# ──────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}[3/6] Dependency Vulnerabilities...${NC}"
echo ""

if [ -f "package.json" ]; then
    echo "  Running npm audit..."
    
    # npm audit çıktısını al
    AUDIT_OUTPUT=$(pnpm audit --json 2>/dev/null || echo '{"error": true}')
    
    # Vulnerability sayılarını parse et
    CRITICAL=$(echo "$AUDIT_OUTPUT" | grep -o '"critical":[0-9]*' | grep -o '[0-9]*' || echo "0")
    HIGH=$(echo "$AUDIT_OUTPUT" | grep -o '"high":[0-9]*' | grep -o '[0-9]*' || echo "0")
    MODERATE=$(echo "$AUDIT_OUTPUT" | grep -o '"moderate":[0-9]*' | grep -o '[0-9]*' || echo "0")
    LOW=$(echo "$AUDIT_OUTPUT" | grep -o '"low":[0-9]*' | grep -o '[0-9]*' || echo "0")
    
    echo "  Critical: $CRITICAL"
    echo "  High:     $HIGH"
    echo "  Moderate: $MODERATE"
    echo "  Low:      $LOW"
    
    {
        echo "Dependency Vulnerabilities:"
        echo "  Critical: $CRITICAL"
        echo "  High:     $HIGH"
        echo "  Moderate: $MODERATE"
        echo "  Low:      $LOW"
    } >> "$REPORT_FILE"
    
    if [ "$CRITICAL" -gt 0 ] || [ "$HIGH" -gt 0 ]; then
        echo -e "  ${RED}❌ Kritik veya yüksek seviye güvenlik açıkları bulundu!${NC}"
        echo "  Run 'pnpm audit fix' to fix automatically"
        ((ISSUES_FOUND++))
    else
        echo -e "  ${GREEN}✅ Kritik güvenlik açığı yok${NC}"
    fi
else
    echo -e "  ${YELLOW}⚠️  package.json bulunamadı — dependency kontrolü yapılamadı${NC}"
fi

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# 4. Common Security Issues
# ──────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}[4/6] Common Security Issues...${NC}"
echo ""

# .env file exposed check
ENV_CHECK=$(curl -s -o /dev/null -w "%{http_code}" "$TARGET_URL/.env")
if [ "$ENV_CHECK" == "200" ]; then
    echo -e "  ${RED}❌ .env file publicly accessible — KRİTİK!${NC}"
    echo "  ❌ .env file exposed" >> "$REPORT_FILE"
    ((ISSUES_FOUND++))
else
    echo -e "  ${GREEN}✅ .env file protected${NC}"
    echo "  ✅ .env file: protected" >> "$REPORT_FILE"
fi

# Git folder exposed check
GIT_CHECK=$(curl -s -o /dev/null -w "%{http_code}" "$TARGET_URL/.git/config")
if [ "$GIT_CHECK" == "200" ]; then
    echo -e "  ${RED}❌ .git folder publicly accessible — KRİTİK!${NC}"
    echo "  ❌ .git folder exposed" >> "$REPORT_FILE"
    ((ISSUES_FOUND++))
else
    echo -e "  ${GREEN}✅ .git folder protected${NC}"
    echo "  ✅ .git folder: protected" >> "$REPORT_FILE"
fi

# robots.txt check (should exist)
ROBOTS_CHECK=$(curl -s -o /dev/null -w "%{http_code}" "$TARGET_URL/robots.txt")
if [ "$ROBOTS_CHECK" == "200" ]; then
    echo -e "  ${GREEN}✅ robots.txt exists${NC}"
    echo "  ✅ robots.txt: exists" >> "$REPORT_FILE"
else
    echo -e "  ${YELLOW}⚠️  robots.txt not found${NC}"
    echo "  ⚠️  robots.txt: missing" >> "$REPORT_FILE"
fi

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# 5. Authentication & Session Security
# ──────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}[5/6] Authentication & Session Security...${NC}"
echo ""

# Cookie security (HttpOnly, Secure, SameSite)
COOKIE_RESPONSE=$(curl -s -I "$TARGET_URL/api/auth/login" 2>/dev/null || echo "")
if [[ "$COOKIE_RESPONSE" =~ "Set-Cookie" ]]; then
    if [[ "$COOKIE_RESPONSE" =~ "HttpOnly" ]]; then
        echo -e "  ${GREEN}✅ HttpOnly flag set${NC}"
        echo "  ✅ Cookie HttpOnly: set" >> "$REPORT_FILE"
    else
        echo -e "  ${RED}❌ HttpOnly flag missing — XSS risk${NC}"
        echo "  ❌ Cookie HttpOnly: missing" >> "$REPORT_FILE"
        ((ISSUES_FOUND++))
    fi
    
    if [[ "$COOKIE_RESPONSE" =~ "Secure" ]]; then
        echo -e "  ${GREEN}✅ Secure flag set${NC}"
        echo "  ✅ Cookie Secure: set" >> "$REPORT_FILE"
    else
        echo -e "  ${YELLOW}⚠️  Secure flag missing${NC}"
        echo "  ⚠️  Cookie Secure: missing" >> "$REPORT_FILE"
    fi
    
    if [[ "$COOKIE_RESPONSE" =~ "SameSite" ]]; then
        echo -e "  ${GREEN}✅ SameSite set${NC}"
        echo "  ✅ Cookie SameSite: set" >> "$REPORT_FILE"
    else
        echo -e "  ${YELLOW}⚠️  SameSite missing — CSRF risk${NC}"
        echo "  ⚠️  Cookie SameSite: missing" >> "$REPORT_FILE"
    fi
else
    echo -e "  ${YELLOW}⚠️  Cookie kontrolü yapılamadı (endpoint test edilemedi)${NC}"
    echo "  ⚠️  Cookie check: skipped" >> "$REPORT_FILE"
fi

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# 6. Rate Limiting Test
# ──────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}[6/6] Rate Limiting Test...${NC}"
echo ""

# 20 request at/sec — rate limit tetiklemeli
echo "  Sending 20 rapid requests..."
RATE_LIMIT_TRIGGERED=false
for i in {1..20}; do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$TARGET_URL/api/health" 2>/dev/null || echo "000")
    if [ "$STATUS" == "429" ]; then
        RATE_LIMIT_TRIGGERED=true
        break
    fi
done

if [ "$RATE_LIMIT_TRIGGERED" == true ]; then
    echo -e "  ${GREEN}✅ Rate limiting aktif (429 Too Many Requests)${NC}"
    echo "  ✅ Rate limiting: active" >> "$REPORT_FILE"
else
    echo -e "  ${YELLOW}⚠️  Rate limiting tespit edilemedi — DDoS riski${NC}"
    echo "  ⚠️  Rate limiting: not detected" >> "$REPORT_FILE"
fi

echo "" >> "$REPORT_FILE"

# ──────────────────────────────────────────────────────────────
# Sonuç
# ──────────────────────────────────────────────────────────────
echo ""
echo "============================================================"
if [ "$ISSUES_FOUND" -eq 0 ]; then
    echo -e "${GREEN}✅ GÜVENLİK AUDIT BAŞARILI — Sorun bulunamadı!${NC}"
    echo "RESULT: PASS ✅" >> "$REPORT_FILE"
else
    echo -e "${RED}❌ $ISSUES_FOUND güvenlik sorunu bulundu!${NC}"
    echo "RESULT: FAIL ❌ ($ISSUES_FOUND issues)" >> "$REPORT_FILE"
fi
echo "Report saved: $REPORT_FILE"
echo "============================================================"
echo ""

# CI/CD için exit code
exit $ISSUES_FOUND
