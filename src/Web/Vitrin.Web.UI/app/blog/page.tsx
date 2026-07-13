import Link from "next/link";
import { ArrowRight, Clock, User } from "lucide-react";

export const metadata = {
  title: "Blog — Vitrin",
  description: "Vitrin ekibinden haberler, ipuçları ve Türk teknoloji ekosisteminden hikayeler.",
};

const posts = [
  {
    slug: "vitrin-nasil-calisir",
    title: "Vitrin Nasıl Çalışır? Oylamanın Arkasındaki Mantık",
    excerpt: "Her gün yüzlerce ürün ekleniyor, ama nasıl öne çıkıyor? Oy algoritması ve topluluk gücü hakkında her şey.",
    category: "Platform",
    date: "12 Temmuz 2026",
    readTime: "4 dk",
    author: "Vitrin Ekibi",
  },
  {
    slug: "maker-rehberi",
    title: "Başarılı Bir Ürün Lansmanı İçin 7 İpucu",
    excerpt: "Vitrin'de öne çıkan ürünlerin ortak özelliklerini inceledik. İşte maker'lar için pratik rehber.",
    category: "Rehber",
    date: "8 Temmuz 2026",
    readTime: "6 dk",
    author: "Vitrin Ekibi",
  },
  {
    slug: "turkiye-tech-ekosistemi",
    title: "2026'da Türkiye Tech Ekosistemi: Sayılarla Bir Bakış",
    excerpt: "Bu yıl Vitrin'de en çok ilgi gören kategoriler, en aktif şehirler ve büyüyen maker topluluğu.",
    category: "Analiz",
    date: "1 Temmuz 2026",
    readTime: "8 dk",
    author: "Vitrin Ekibi",
  },
  {
    slug: "acik-kaynak-turkiye",
    title: "Türkiye'den Dünyaya: Açık Kaynak Başarı Hikayeleri",
    excerpt: "Vitrin'de keşfedilen ve global ilgi gören Türk açık kaynak projelerini derledik.",
    category: "Hikaye",
    date: "24 Haziran 2026",
    readTime: "5 dk",
    author: "Vitrin Ekibi",
  },
];

const categories = ["Tümü", "Platform", "Rehber", "Analiz", "Hikaye"];

export default function BlogPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-5xl px-4 py-16 text-center">
          <h1 className="text-4xl sm:text-5xl font-extrabold text-foreground mb-4">Blog</h1>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Türk teknoloji ekosistemi, maker hikayeleri ve platform haberleri.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-5xl px-4 py-12">
        {/* Categories */}
        <div className="flex gap-2 flex-wrap mb-10">
          {categories.map((cat) => (
            <button
              key={cat}
              className={`px-4 py-1.5 rounded-full text-sm font-medium border transition-colors ${
                cat === "Tümü"
                  ? "bg-emerald-500 text-white border-emerald-500"
                  : "border-border text-muted-foreground hover:border-emerald-500/30 hover:text-foreground"
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* Featured Post */}
        <div className="bg-card border border-border rounded-3xl overflow-hidden mb-8 hover:border-emerald-500/30 transition-colors group">
          <div className="h-48 bg-gradient-to-br from-emerald-500/20 via-emerald-500/5 to-transparent" />
          <div className="p-8">
            <span className="text-xs font-semibold text-emerald-500 uppercase tracking-wider">
              {posts[0].category}
            </span>
            <h2 className="text-2xl font-extrabold text-foreground mt-2 mb-3 group-hover:text-emerald-500 transition-colors">
              {posts[0].title}
            </h2>
            <p className="text-muted-foreground mb-6">{posts[0].excerpt}</p>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4 text-xs text-muted-foreground">
                <span className="flex items-center gap-1"><User className="w-3 h-3" />{posts[0].author}</span>
                <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{posts[0].readTime}</span>
                <span>{posts[0].date}</span>
              </div>
              <Link
                href={`/blog/${posts[0].slug}`}
                className="flex items-center gap-1 text-sm font-semibold text-emerald-500 hover:gap-2 transition-all"
              >
                Oku <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>
        </div>

        {/* Post Grid */}
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {posts.slice(1).map((post) => (
            <Link
              key={post.slug}
              href={`/blog/${post.slug}`}
              className="bg-card border border-border rounded-2xl p-6 hover:border-emerald-500/30 transition-colors group flex flex-col"
            >
              <div className="h-24 bg-gradient-to-br from-emerald-500/10 to-transparent rounded-xl mb-4" />
              <span className="text-xs font-semibold text-emerald-500 uppercase tracking-wider mb-2">
                {post.category}
              </span>
              <h3 className="font-bold text-foreground group-hover:text-emerald-500 transition-colors mb-2 flex-1">
                {post.title}
              </h3>
              <p className="text-sm text-muted-foreground mb-4 line-clamp-2">{post.excerpt}</p>
              <div className="flex items-center gap-3 text-xs text-muted-foreground">
                <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{post.readTime}</span>
                <span>{post.date}</span>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </main>
  );
}
