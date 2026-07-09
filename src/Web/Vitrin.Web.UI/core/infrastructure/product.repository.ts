import apiClient from './api-client';

export const ProductRepository = {
  async getProducts() {
    const response = await apiClient.get('/products');
    return response.data;
  },
  
  async upvoteProduct(productId: string, token: string) {
    const response = await apiClient.post(`/products/${productId}/vote`, {}, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  },

  async getMyVotes(token: string) {
    const response = await apiClient.get(`/products/my-votes`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  }
};
