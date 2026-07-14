import { create } from 'zustand';
import { Product, ProductApiModel, Topic } from '../domain/product.types';
import { ProductRepository } from '../infrastructure/product.repository';
import { getErrorMessage } from '@/lib/errors';

interface ProductStore {
  products: Product[];
  makerProducts: Product[];
  upvotedProducts: Product[];
  topics: Topic[];
  selectedTopicSlug: string | null;
  isLoading: boolean;
  error: string | null;
  votedProductIds: string[];
  fetchProducts: (topicSlug?: string) => Promise<void>;
  fetchMakerProducts: (makerId: string) => Promise<void>;
  fetchUpvotedProducts: (token: string) => Promise<void>;
  fetchTopics: () => Promise<void>;
  setTopicFilter: (topicSlug: string | null) => void;
  fetchMyVotes: (token: string) => Promise<void>;
  upvote: (productId: string, token: string) => Promise<void>;
}

export const useProductStore = create<ProductStore>((set, get) => ({
  products: [],
  makerProducts: [],
  upvotedProducts: [],
  topics: [],
  selectedTopicSlug: null,
  votedProductIds: [],
  isLoading: false,
  error: null,
  
  setTopicFilter: (topicSlug) => {
    set({ selectedTopicSlug: topicSlug });
    get().fetchProducts(topicSlug || undefined);
  },

  fetchTopics: async () => {
    try {
      const data = await ProductRepository.getTopics();
      set({ topics: data });
    } catch (error) {
      console.error("Failed to fetch topics", error);
    }
  },

  fetchProducts: async (topicSlug?: string) => {
    set({ isLoading: true, error: null });
    try {
      const data = await ProductRepository.getProducts(topicSlug);
      
      // BLoC / Store Layer: Map Backend Entity to Frontend Domain Entity
      // Bu katman veriyi UI'ın anlayacağı hale getirir (Business Logic)
      const mappedProducts: Product[] = data.map((p: ProductApiModel, index: number) => ({
        id: p.id,
        rank: index + 1,
        name: p.name,
        slug: p.slug,
        description: p.tagline || p.description,
        publishedAt: p.publishedAt,
        image: p.thumbnailUrl || '/products/notai.png',
        topics: p.topics || [],
        votes: p.upvotes || 0, // Gerçek upvote sayısı backend'den dönüyor
      }));
      
      set({ products: mappedProducts, isLoading: false });
    } catch (error: unknown) {
      set({ error: getErrorMessage(error, 'Ürünler yüklenirken hata oluştu.'), isLoading: false });
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

  fetchMakerProducts: async (makerId: string) => {
    set({ isLoading: true, error: null });
    try {
      const data = await ProductRepository.getMakerProducts(makerId);
      const mappedProducts: Product[] = data.map((p: ProductApiModel, index: number) => ({
        id: p.id,
        rank: index + 1,
        name: p.name,
        slug: p.slug,
        description: p.tagline || p.description,
        publishedAt: p.publishedAt,
        image: p.thumbnailUrl || '/products/notai.png',
        topics: p.topics || [],
        votes: p.upvotes || 0,
      }));
      set({ makerProducts: mappedProducts, isLoading: false });
    } catch (error: unknown) {
      set({ error: getErrorMessage(error, 'Ürünler yüklenirken hata oluştu.'), isLoading: false });
    }
  },

  fetchUpvotedProducts: async (token: string) => {
    set({ isLoading: true, error: null });
    try {
      const data = await ProductRepository.getUpvotedProducts(token);
      const mappedProducts: Product[] = data.map((p: ProductApiModel, index: number) => ({
        id: p.id,
        rank: index + 1,
        name: p.name,
        slug: p.slug,
        description: p.tagline || p.description,
        publishedAt: p.publishedAt,
        image: p.thumbnailUrl || '/products/notai.png',
        topics: p.topics || [],
        votes: p.upvotes || 0,
      }));
      set({ upvotedProducts: mappedProducts, isLoading: false });
    } catch (error: unknown) {
      set({ error: getErrorMessage(error, 'Oylanan ürünler yüklenirken hata oluştu.'), isLoading: false });
    }
  },

  upvote: async (productId: string, token: string) => {
    const hadVoted = get().votedProductIds.includes(productId);

    try {
      // Optimistic UI Update
      set((state) => {
        const newVotedIds = hadVoted
          ? state.votedProductIds.filter(id => id !== productId)
          : [...state.votedProductIds, productId];

        return {
          votedProductIds: newVotedIds,
          products: state.products.map(p => 
            p.id === productId ? { ...p, votes: hadVoted ? p.votes - 1 : p.votes + 1 } : p
          ),
          makerProducts: state.makerProducts.map(p => 
            p.id === productId ? { ...p, votes: hadVoted ? p.votes - 1 : p.votes + 1 } : p
          ),
          upvotedProducts: state.upvotedProducts.map(p => 
            p.id === productId ? { ...p, votes: hadVoted ? p.votes - 1 : p.votes + 1 } : p
          )
        };
      });

      // API Call
      await ProductRepository.toggleVote(productId, hadVoted, token);
      
      // Gamification is recorded only when a new vote is added.
      if (!hadVoted) {
        try {
          const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
          await fetch(`${apiUrl}/api/auth/users/me/record-vote`, {
            method: "POST",
            headers: { Authorization: `Bearer ${token}` }
          });
        } catch (err) {
          console.error("Streak could not be updated", err);
        }
      }
      
      // Sync upvoted products list in the background
      get().fetchUpvotedProducts(token);
    } catch (error) {
      console.error("Oy verme işlemi başarısız oldu", error);
      set((state) => ({
        votedProductIds: hadVoted
          ? [...new Set([...state.votedProductIds, productId])]
          : state.votedProductIds.filter(id => id !== productId),
        products: state.products.map(product =>
          product.id === productId
            ? { ...product, votes: Math.max(0, product.votes + (hadVoted ? 1 : -1)) }
            : product
        ),
        makerProducts: state.makerProducts.map(product =>
          product.id === productId
            ? { ...product, votes: Math.max(0, product.votes + (hadVoted ? 1 : -1)) }
            : product
        ),
        upvotedProducts: state.upvotedProducts.map(product =>
          product.id === productId
            ? { ...product, votes: Math.max(0, product.votes + (hadVoted ? 1 : -1)) }
            : product
        ),
      }));
    }
  }
}));
