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
