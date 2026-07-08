export interface Product {
  id: string;
  rank?: number;
  name: string;
  description: string;
  image: string;
  tags: string[];
  votes: number;
}
