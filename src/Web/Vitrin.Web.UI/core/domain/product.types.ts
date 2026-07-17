export interface Product {
  id: string;
  rank?: number;
  name: string;
  slug: string;
  description: string;
  publishedAt: string;
  image: string;
  topics?: { id: string; name: string; slug: string }[];
  votes: number;
  views?: number;
  comments?: number;
  trendScore?: number;
  searchScore?: number;
  matchType?: string | null;
}

export interface Topic {
  id: string;
  name: string;
  slug: string;
}

export interface ProductApiModel {
  id: string;
  name: string;
  slug: string;
  tagline?: string | null;
  description: string;
  publishedAt: string;
  thumbnailUrl?: string | null;
  topics?: Topic[];
  upvotes?: number;
  viewCount?: number;
  commentCount?: number;
  trendScore?: number;
  searchScore?: number;
  matchType?: string | null;
}

export interface ProductDetailApiModel extends ProductApiModel {
  makerId: string;
  tagline: string;
  thumbnailUrl?: string | null;
  galleryUrls?: string[];
  upvotes: number;
}

export interface CursorPage<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

export type ProductSort = 'newest' | 'trending' | 'most_voted' | 'most_commented' | 'most_viewed';

export interface ProductFilters {
  q?: string;
  topics?: string[];
  minUpvotes?: number;
  minComments?: number;
  minViews?: number;
  publishedFrom?: string;
  publishedTo?: string;
  sort?: ProductSort;
}
