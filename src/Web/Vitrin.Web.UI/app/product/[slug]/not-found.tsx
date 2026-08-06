import Link from "next/link";
import { PackageSearch } from "lucide-react";

export default function ProductNotFound() {
  return <main className="mx-auto flex min-h-[65vh] max-w-2xl flex-col items-center justify-center px-4 text-center"><div className="rounded-3xl bg-muted p-5"><PackageSearch className="h-12 w-12 text-muted-foreground" /></div><h1 className="mt-6 text-3xl font-black">Ürün bulunamadı</h1><p className="mt-2 text-muted-foreground">Ürün kaldırılmış, arşivlenmiş veya bağlantısı değişmiş olabilir.</p><Link href="/discover" className="mt-6 rounded-xl bg-emerald-600 px-5 py-3 font-bold text-white hover:bg-emerald-700">Ürünleri keşfet</Link></main>;
}
