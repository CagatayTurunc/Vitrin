import { ImageResponse } from "next/og";

// Global fallback OG image — tüm sayfalarda override edilmemişse bu kullanılır.
// 1200×630px: Facebook, Twitter, LinkedIn standart boyutu.
export const runtime = "edge";
export const alt = "Vitrin — Türkiye'nin Ürün Keşif Platformu";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OgImage() {
  return new ImageResponse(
    (
      <div
        style={{
          background: "linear-gradient(135deg, #f0fdf4 0%, #ecfdf5 40%, #f0f9ff 100%)",
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          fontFamily: "sans-serif",
          position: "relative",
          overflow: "hidden",
        }}
      >
        {/* Arka plan dekorasyon */}
        <div
          style={{
            position: "absolute",
            top: -100,
            right: -100,
            width: 400,
            height: 400,
            borderRadius: "50%",
            background: "rgba(16, 185, 129, 0.12)",
          }}
        />
        <div
          style={{
            position: "absolute",
            bottom: -80,
            left: -80,
            width: 300,
            height: 300,
            borderRadius: "50%",
            background: "rgba(16, 185, 129, 0.08)",
          }}
        />

        {/* Logo icon */}
        <div
          style={{
            width: 80,
            height: 80,
            borderRadius: 20,
            background: "linear-gradient(135deg, #10b981, #059669)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            marginBottom: 32,
            boxShadow: "0 20px 40px rgba(16, 185, 129, 0.3)",
          }}
        >
          <svg width="44" height="44" viewBox="0 0 24 24" fill="none">
            <path
              d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"
              stroke="white"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>

        {/* Başlık */}
        <div
          style={{
            fontSize: 72,
            fontWeight: 900,
            color: "#111827",
            letterSpacing: "-2px",
            lineHeight: 1.1,
            marginBottom: 16,
          }}
        >
          Vitrin
        </div>

        {/* Alt başlık */}
        <div
          style={{
            fontSize: 28,
            color: "#6b7280",
            fontWeight: 500,
            textAlign: "center",
            maxWidth: 700,
            lineHeight: 1.4,
          }}
        >
          Türkiye&apos;nin en yeni ve en iyi ürünlerini keşfet, oy ver, paylaş.
        </div>

        {/* URL badge */}
        <div
          style={{
            marginTop: 40,
            background: "rgba(16, 185, 129, 0.1)",
            border: "2px solid rgba(16, 185, 129, 0.3)",
            borderRadius: 50,
            padding: "10px 28px",
            fontSize: 20,
            color: "#059669",
            fontWeight: 700,
          }}
        >
          vitrin.it.com
        </div>
      </div>
    ),
    { ...size }
  );
}
