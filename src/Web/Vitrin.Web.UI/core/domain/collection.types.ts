import type { ProductApiModel } from './product.types';

export interface CollectionSummary {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  userId: string;
  productCount: number;
}

export interface CollectionDetail extends CollectionSummary {
  createdAt: string;
  products?: ProductApiModel[];
}
