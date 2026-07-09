import { create } from 'zustand';
import { Product } from '../domain/product.types';
import { ProductRepository } from '../infrastructure/product.repository';

interface ProductStore {
  products: Product[];
  isLoading: boolean;
  error: string | null;
  votedProductIds: string[];
  fetchProducts: () => Promise<void>;
  fetchMyVotes: (token: string) => Promise<void>;
  upvote: (productId: string, token: string) => Promise<void>;
}

export const useProductStore = create<ProductStore>((set, get) => ({
  products: [],
  votedProductIds: [],
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
        description: p.tagline || p.description,
        image: p.thumbnailUrl || '/products/notai.png',
        topics: p.topics || [],
        votes: p.upvotes || 0, // Gerçek upvote sayısı backend'den dönüyor
      }));
      
      set({ products: mappedProducts, isLoading: false });
    } catch (error: any) {
      set({ error: error.message || 'Ürünler yüklenirken hata oluştu.', isLoading: false });
    }
  },
  
  fetchMyVotes: async (token: string) => {
    try {
      const data = await ProductRepository.getMyVotes(token);
      set({ votedProductIds: data });
    } catch (error) {
      console.error("Failed to fetch my votes", error);
    }
  },

  upvote: async (productId: string, token: string) => {
    try {
      // Optimistic UI Update
      set((state) => {
        const hasVoted = state.votedProductIds.includes(productId);
        const newVotedIds = hasVoted
          ? state.votedProductIds.filter(id => id !== productId)
          : [...state.votedProductIds, productId];

        return {
          votedProductIds: newVotedIds,
          products: state.products.map(p => 
            p.id === productId ? { ...p, votes: hasVoted ? p.votes - 1 : p.votes + 1 } : p
          )
        };
      });

      // API Call
      await ProductRepository.upvoteProduct(productId, token);
    } catch (error) {
      console.error("Oy verme işlemi başarısız oldu", error);
      // Revert optimistic update here if needed
    }
  }
}));
