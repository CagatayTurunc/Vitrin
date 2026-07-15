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
