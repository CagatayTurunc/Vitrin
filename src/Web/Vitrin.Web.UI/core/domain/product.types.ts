export interface Product {
  id: string;
  rank?: number;
  name: string;
  description: string;
  image: string;
  topics?: { id: string; name: string; slug: string }[];
  votes: number;
}
