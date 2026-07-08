import { create } from 'zustand';
import { Product } from '../domain/product.types';
import { ProductRepository } from '../infrastructure/product.repository';

interface ProductStore {
  products: Product[];
  isLoading: boolean;
  error: string | null;
  fetchProducts: () => Promise<void>;
  upvote: (productId: string) => Promise<void>;
}

export const useProductStore = create<ProductStore>((set, get) => ({
  products: [],
  isLoading: false,
  error: null,
  
  fetchProducts: async () => {
    set({ isLoading: true, error: null });
    try {
      const data = await ProductRepository.getProducts();
      
      // BLoC / Store Layer: Map Backend Entity to Frontend Domain Entity
      // Bu katman veriyi UI'ın anlayacağı hale getirir (Business Logic)
      const mappedProducts: Product[] = data.map((p: any, index: number) => ({
        id: p.id,
        rank: index + 1,
        name: p.name,
        description: p.description,
        image: p.imageUrl || '/products/notai.png',
        tags: ['SaaS', 'İnovasyon'], // Şimdilik mock etiketler, ileride AI servisinden gelecek
        votes: Math.floor(Math.random() * 100), // Şimdilik rastgele oy sayısı
      }));
      
      set({ products: mappedProducts, isLoading: false });
    } catch (error: any) {
      set({ error: error.message || 'Ürünler yüklenirken hata oluştu.', isLoading: false });
    }
  },
  
  upvote: async (productId: string) => {
    try {
      // API İsteği atılıyor...
      // await ProductRepository.upvoteProduct(productId);
      
      // Optimistic UI Update (Kullanıcı beklemesin diye arayüzde anında artırıyoruz)
      set((state) => ({
        products: state.products.map(p => 
          p.id === productId ? { ...p, votes: p.votes + 1 } : p
        )
      }));
    } catch (error) {
      console.error("Oy verme işlemi başarısız oldu", error);
    }
  }
}));
