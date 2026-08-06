import { notFound } from "next/navigation";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

interface BadgePageProps {
  params: {
    productId: string;
  };
  searchParams: {
    theme?: string;
  };
}

export default async function BadgePage({ params, searchParams }: BadgePageProps) {
  const { productId } = params;
  const theme = searchParams.theme === "dark" ? "dark" : "light";

  let product = null;
  try {
    const res = await fetch(`${API_URL}/api/products/${productId}`, {
      next: { revalidate: 60 }
    });
    if (res.ok) {
      product = await res.json();
    }
  } catch (error) {
    console.error("Badge data fetch error:", error);
  }

  if (!product) {
    return notFound();
  }

  const isDark = theme === "dark";

  return (
    <>
      <style dangerouslySetInnerHTML={{ __html: 'header, footer { display: none !important; } main { padding: 0 !important; margin: 0 !important; min-height: 100vh !important; display: flex; align-items: center; justify-content: center; } body { background: transparent !important; }' }} />
      <a 
        href={`https://vitrin.com/product/${product.slug}?utm_source=badge`}
        target="_blank"
        rel="noreferrer"
        className={`flex items-center justify-between w-[250px] h-[54px] rounded-xl border p-2 transition-all hover:scale-[1.02] ${
          isDark 
            ? "bg-slate-950 border-slate-800 text-white" 
            : "bg-white border-slate-200 text-slate-900"
        }`}
        style={{ textDecoration: 'none', fontFamily: 'system-ui, sans-serif' }}
      >
        <div className="flex flex-col justify-center px-2">
          <span className="text-[10px] font-semibold tracking-wider uppercase text-emerald-500 mb-0.5">
            Vitrin'de Yayında
          </span>
          <span className="text-sm font-bold truncate max-w-[140px]">
            {product.name}
          </span>
        </div>
        <div className={`flex flex-col items-center justify-center rounded-lg px-3 py-1.5 min-w-[50px] ${
          isDark ? "bg-slate-900" : "bg-slate-50"
        }`}>
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="text-emerald-500 mb-0.5"><path d="m18 15-6-6-6 6"/></svg>
          <span className="text-sm font-black tabular-nums leading-none">
            {product.upvotes || 0}
          </span>
        </div>
      </a>
    </>
  );
}
