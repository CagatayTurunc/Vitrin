import apiClient from './api-client';

export const ProductRepository = {
  async getProducts() {
    const response = await apiClient.get('/products');
    return response.data;
  },
  
  async upvoteProduct(productId: string) {
    // API Gateway handles routing this to Voting Service
    const response = await apiClient.post(`/votes`);
    // Assuming the Voting API expects a payload like { ProductId: ... }
    // Or we need to pass the payload correctly:
    // await apiClient.post('/votes', { ProductId: productId, UserId: "user-123" });
    return response.data;
  }
};
