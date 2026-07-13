import Link from "next/link";
import { Sparkles } from "lucide-react";

const footerLinks = {
  vitrin: [
    { title: "Hakkımızda", href: "/about" },
    { title: "Kariyer", href: "/careers" },
    { title: "Blog", href: "/blog" },
    { title: "İletişim", href: "/contact" },
  ],
  products: [
    { title: "Yapay Zeka", href: "/topic/yapay-zeka" },
    { title: "Tasarım", href: "/topic/tasarim" },
    { title: "Geliştirici Araçları", href: "/topic/gelistirici-araclari" },
    { title: "Açık Kaynak", href: "/topic/acik-kaynak" },
    { title: "SaaS", href: "/topic/saas" },
  ],
  community: [
    { title: "Liderlik Tablosu", href: "/leaderboard" },
    { title: "Tartışmalar", href: "/discussions" },
    { title: "Topluluk Kuralları", href: "/rules" },
    { title: "Etkinlikler", href: "/events" },
  ],
  legal: [
    { title: "Kullanım Koşulları", href: "/terms" },
    { title: "Gizlilik Politikası", href: "/privacy" },
    { title: "Çerez Politikası", href: "/cookies" },
    { title: "KVKK Aydınlatma Metni", href: "/kvkk" },
  ],
};

export function SiteFooter() {
  return (
    <footer className="w-full border-t border-border bg-background/50 backdrop-blur-sm mt-auto">
      <div className="mx-auto max-w-7xl px-4 py-12 md:py-16 sm:px-6">
        <div className="grid grid-cols-2 gap-8 md:grid-cols-4 lg:grid-cols-5">
          {/* Logo & Description */}
          <div className="col-span-2 lg:col-span-1">
            <Link href="/" className="flex shrink-0 items-center gap-2 mb-4">
              <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm">
                <Sparkles className="h-4 w-4" aria-hidden="true" />
              </span>
              <span className="font-sans text-xl font-extrabold tracking-tight text-foreground">
                Vitrin
              </span>
            </Link>
            <p className="text-sm text-muted-foreground mb-6">
              En yeni ürünleri keşfedin, oy verin ve favori geliştiricilerinizi destekleyin. Türkiye'nin ürün avcıları topluluğu.
            </p>
            <div className="flex items-center gap-3">
              <a
                href="https://twitter.com"
                target="_blank"
                rel="noreferrer"
                aria-label="Twitter"
                className="w-8 h-8 rounded-lg border border-border flex items-center justify-center text-muted-foreground hover:text-emerald-500 hover:border-emerald-500/30 transition-colors"
              >
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
                </svg>
              </a>
              <a
                href="https://github.com"
                target="_blank"
                rel="noreferrer"
                aria-label="GitHub"
                className="w-8 h-8 rounded-lg border border-border flex items-center justify-center text-muted-foreground hover:text-emerald-500 hover:border-emerald-500/30 transition-colors"
              >
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path fillRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clipRule="evenodd" />
                </svg>
              </a>
              <a
                href="https://linkedin.com"
                target="_blank"
                rel="noreferrer"
                aria-label="LinkedIn"
                className="w-8 h-8 rounded-lg border border-border flex items-center justify-center text-muted-foreground hover:text-emerald-500 hover:border-emerald-500/30 transition-colors"
              >
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path fillRule="evenodd" d="M19 0h-14c-2.761 0-5 2.239-5 5v14c0 2.761 2.239 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-11 19h-3v-11h3v11zm-1.5-12.268c-.966 0-1.75-.79-1.75-1.764s.784-1.764 1.75-1.764 1.75.79 1.75 1.764-.783 1.764-1.75 1.764zm13.5 12.268h-3v-5.604c0-3.368-4-3.113-4 0v5.604h-3v-11h3v1.765c1.396-2.586 7-2.777 7 2.476v6.759z" clipRule="evenodd" />
                </svg>
              </a>
            </div>
          </div>

          {/* Vitrin Links */}
          <div className="lg:pl-8">
            <h3 className="mb-4 text-sm font-semibold text-foreground">Vitrin</h3>
            <ul className="space-y-3">
              {footerLinks.vitrin.map((link) => (
                <li key={link.title}>
                  <Link
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-emerald-500 transition-colors"
                  >
                    {link.title}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Category Links */}
          <div>
            <h3 className="mb-4 text-sm font-semibold text-foreground">Kategoriler</h3>
            <ul className="space-y-3">
              {footerLinks.products.map((link) => (
                <li key={link.title}>
                  <Link
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-emerald-500 transition-colors"
                  >
                    {link.title}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Community Links */}
          <div>
            <h3 className="mb-4 text-sm font-semibold text-foreground">Topluluk</h3>
            <ul className="space-y-3">
              {footerLinks.community.map((link) => (
                <li key={link.title}>
                  <Link
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-emerald-500 transition-colors"
                  >
                    {link.title}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Legal Links */}
          <div>
            <h3 className="mb-4 text-sm font-semibold text-foreground">Yasal</h3>
            <ul className="space-y-3">
              {footerLinks.legal.map((link) => (
                <li key={link.title}>
                  <Link
                    href={link.href}
                    className="text-sm text-muted-foreground hover:text-emerald-500 transition-colors"
                  >
                    {link.title}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Bottom bar */}
        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-border pt-8 sm:flex-row">
          <p className="text-xs text-muted-foreground">
            &copy; {new Date().getFullYear()} Vitrin. Tüm hakları saklıdır.
          </p>
          <div className="flex items-center gap-4">
            <Link href="/terms" className="text-xs text-muted-foreground hover:text-emerald-500 transition-colors">
              Koşullar
            </Link>
            <Link href="/privacy" className="text-xs text-muted-foreground hover:text-emerald-500 transition-colors">
              Gizlilik
            </Link>
            <Link href="/cookies" className="text-xs text-muted-foreground hover:text-emerald-500 transition-colors">
              Çerezler
            </Link>
            <Link href="/kvkk" className="text-xs text-muted-foreground hover:text-emerald-500 transition-colors">
              KVKK
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}
