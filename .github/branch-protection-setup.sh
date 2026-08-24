#!/bin/bash
# ============================================================
# Branch Protection Rules Setup
# Çalıştırmak için: bash .github/branch-protection-setup.sh
# Gereksinim: gh CLI kurulu ve `gh auth login` yapılmış olmalı
# ============================================================

REPO="CagatayTurunc/Vitrin"

echo "🔒 main branch protection kuruluyor..."

gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/$REPO/branches/main/protection \
  --input - <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "Backend Tests",
      "Frontend Checks",
      "Security Scan"
    ]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": false
  },
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_linear_history": false,
  "required_conversation_resolution": true
}
EOF

echo "✅ main branch protection aktif:"
echo "   - PR zorunlu (en az 1 onay)"
echo "   - CI check'leri geçmeden merge yok"
echo "   - Direkt push engellendi"
echo "   - Force push engellendi"
echo ""

echo "🌿 develop branch oluşturuluyor (yoksa)..."
gh api \
  --method POST \
  -H "Accept: application/vnd.github+json" \
  /repos/$REPO/git/refs \
  -f ref="refs/heads/develop" \
  -f sha="$(gh api /repos/$REPO/branches/main --jq '.commit.sha')" \
  2>/dev/null || echo "   develop branch zaten mevcut"

echo ""
echo "🎉 Kurulum tamamlandı!"
echo ""
echo "Bundan sonra geliştirme akışı:"
echo "  1. develop branch'ından feature branch aç"
echo "  2. Değişikliklerini yap, commit et"
echo "  3. develop'a PR aç → CI otomatik çalışır"
echo "  4. develop → main PR aç → 1 onay + CI gerekli"
echo "  5. main'e merge → deploy.yml otomatik tetiklenir"
