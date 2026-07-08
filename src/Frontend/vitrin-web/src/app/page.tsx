import React from 'react';

// Next.js Server Component cache'i devre dışı bırakmak için
export const dynamic = 'force-dynamic';

export default async function Home() {
  let products = [];
  
  try {
    const res = await fetch('http://localhost:5000/api/products', { cache: 'no-store' });
    if (res.ok) {
      const data = await res.json();
      // Veritabanı verisini UI modeline eşle
      products = data.map((p: any, index: number) => ({
        id: p.id,
        name: p.name,
        description: p.description,
        tags: ["Geliştirici", "Teknoloji"], // Gerçek sistemde db'den gelecek
        votes: Math.floor(Math.random() * 100) + 50, // Geçici olarak random vote sayısı
        logoColor: ["bg-indigo-500", "bg-green-600", "bg-orange-500", "bg-slate-800", "bg-blue-500"][index % 5],
        icon: p.name.charAt(0).toUpperCase()
      }));
    }
  } catch (error) {
    console.log("API'ye ulaşılamadı veya hata oluştu:", error);
  }

  // Eğer DB'den hiç ürün dönmezse (henüz eklenmediyse) sahte (mock) verileri gösterelim:
  if (!products || products.length === 0) {
    products = [
      {
        id: 1,
        name: "NotAI",
        description: "Toplantılarını otomatik özetleyen yapay zeka destekli not defteri.",
        tags: ["Yapay Zeka", "Verimlilik", "Ücretsiz"],
        votes: 342,
        logoColor: "bg-gradient-to-br from-indigo-500 to-purple-600",
        icon: "✨"
      },
      {
        id: 2,
        name: "FinTrack",
        description: "Harcamalarını tek ekranda takip et, bütçeni akıllıca yönet.",
        tags: ["Finans", "SaaS", "Mobil"],
        votes: 271,
        logoColor: "bg-green-600",
        icon: "📈"
      },
      {
        id: 3,
        name: "PixelBoard",
        description: "Ekiplerin gerçek zamanlı tasarım yaptığı sınırsız beyaz tahta.",
        tags: ["Tasarım", "İş Birliği", "SaaS"],
        votes: 198,
        logoColor: "bg-gradient-to-br from-orange-400 to-pink-500",
        icon: "🎨"
      },
      {
        id: 4,
        name: "CodeMate",
        description: "Kod yazarken bağlamı anlayan yapay zeka çift programlama asistanı.",
        tags: ["Geliştirici", "Yapay Zeka", "Ücretsiz"],
        votes: 156,
        logoColor: "bg-slate-800",
        icon: ">_"
      },
      {
        id: 5,
        name: "Tasky",
        description: "Küçük ekipler için sade ve hızlı görev yönetim uygulaması.",
        tags: ["Verimlilik", "SaaS", "Ekip"],
        votes: 128,
        logoColor: "bg-blue-500",
        icon: "✓"
      }
    ];
  }

  return (
    <div className="min-h-screen">
      {/* Navbar */}
      <header className="border-b border-gray-200 dark:border-[#333333] bg-white dark:bg-[#1a1a1a] sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          
          {/* Logo */}
          <div className="flex items-center gap-2 cursor-pointer">
            <div className="w-8 h-8 rounded-full bg-[#00b074] flex items-center justify-center text-white font-bold text-xl">
              ✧
            </div>
            <span className="font-bold text-xl tracking-tight">Vitrin</span>
          </div>

          {/* Search Bar */}
          <div className="hidden md:flex flex-1 max-w-xl mx-8">
            <div className="relative w-full">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <svg className="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clipRule="evenodd" />
                </svg>
              </div>
              <input 
                type="text" 
                placeholder="Ürün, kategori veya koleksiyon ara..." 
                className="w-full pl-10 pr-4 py-2 bg-gray-50 dark:bg-[#242424] border border-gray-200 dark:border-[#333333] rounded-full text-sm focus:outline-none focus:ring-2 focus:ring-[#00b074] focus:border-transparent transition-all placeholder-gray-500"
              />
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-4">
            <button className="text-sm font-medium px-4 py-2 rounded-full border border-gray-200 dark:border-[#333333] hover:bg-gray-50 dark:hover:bg-[#242424] transition-colors">
              Giriş Yap
            </button>
            <button className="text-sm font-medium px-4 py-2 rounded-full bg-[#00b074] text-white hover:bg-[#009660] transition-colors">
              Ürün Ekle
            </button>
          </div>

        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-4xl mx-auto px-4 py-12">
        
        {/* Header Section */}
        <div className="mb-8">
          <div className="flex items-center gap-2 text-[#00b074] font-medium mb-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <span>8 Temmuz 2026</span>
          </div>
          <h1 className="text-4xl font-bold mb-2">Günün Ürünleri</h1>
          <p className="text-gray-500 dark:text-gray-400 text-lg">
            Topluluğun bugün keşfettiği en yeni ürünler. En sevdiğine oy ver, öne çıkmasına yardım et.
          </p>
        </div>

        {/* Product List Card */}
        <div className="bg-white dark:bg-[#242424] border border-gray-200 dark:border-[#333333] rounded-2xl p-2 sm:p-6 mb-8">
          <div className="flex flex-col gap-6">
            {products.map((product: any, index: number) => (
              <div key={product.id} className="group flex items-start gap-4 p-4 rounded-xl hover:bg-gray-50 dark:hover:bg-[#2a2a2a] transition-all cursor-pointer">
                
                {/* Rank Number */}
                <div className="hidden sm:flex w-6 pt-3 text-gray-400 font-medium justify-center">
                  {index + 1}
                </div>

                {/* App Icon */}
                <div className={`w-16 h-16 rounded-xl flex-shrink-0 flex items-center justify-center text-white text-2xl font-bold shadow-sm ${product.logoColor}`}>
                  {product.icon}
                </div>

                {/* Product Info */}
                <div className="flex-1">
                  <h3 className="font-bold text-lg mb-1 group-hover:text-[#00b074] transition-colors">{product.name}</h3>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mb-3 line-clamp-2">
                    {product.description}
                  </p>
                  
                  {/* Tags */}
                  <div className="flex flex-wrap gap-2">
                    {product.tags.map((tag: string) => (
                      <span key={tag} className="px-3 py-1 bg-gray-100 dark:bg-[#333333] text-gray-600 dark:text-gray-300 rounded-full text-xs font-medium">
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>

                {/* Upvote Button */}
                <div className="flex flex-col items-center justify-center w-16 h-16 rounded-xl border border-gray-200 dark:border-[#333333] hover:border-[#00b074] hover:bg-green-50 dark:hover:bg-[#00b074]/10 transition-colors flex-shrink-0 group-hover:shadow-sm">
                  <svg className="w-5 h-5 text-gray-500 dark:text-gray-400 group-hover:text-[#00b074] mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 15l7-7 7 7" />
                  </svg>
                  <span className="font-bold text-sm text-gray-700 dark:text-gray-200 group-hover:text-[#00b074]">{product.votes}</span>
                </div>

              </div>
            ))}
          </div>
        </div>

        {/* Footer Link */}
        <div className="text-center">
          <span className="text-gray-500 dark:text-gray-400">Daha fazlasını mı arıyorsun? </span>
          <a href="#" className="text-[#00b074] font-medium hover:underline">Tüm ürünleri keşfet</a>
        </div>

      </main>
    </div>
  );
}
