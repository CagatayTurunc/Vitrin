import apiClient from './api-client';

export const ProductRepository = {
  async getProducts(topicSlug?: string) {
    const url = topicSlug ? `/products?topicSlug=${topicSlug}` : '/products';
    const response = await apiClient.get(url);
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
  },

  async getTopics() {
    const response = await apiClient.get('/topics');
    return response.data;
  },

  async getMakerProducts(makerId: string) {
    const response = await apiClient.get(`/products/maker/${makerId}`);
    return response.data;
  },

  async getUpvotedProducts(token: string) {
    const response = await apiClient.get('/products/upvoted', {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  }
};
