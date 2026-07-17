import apiClient from './api-client';
import type { CursorPage, ProductApiModel, ProductFilters, Topic } from '../domain/product.types';

export const ProductRepository = {
  async getProducts(topicSlug?: string, cursor?: string): Promise<CursorPage<ProductApiModel>> {
    const params = new URLSearchParams({ pageSize: '20' });
    if (topicSlug) params.set('topicSlug', topicSlug);
    if (cursor) params.set('cursor', cursor);
    const url = `/products?${params.toString()}`;
    const response = await apiClient.get(url);
    return response.data;
  },

  async filterProducts(filters: ProductFilters, cursor?: string): Promise<CursorPage<ProductApiModel>> {
    const params = new URLSearchParams({ pageSize: '20', sort: filters.sort ?? 'newest' });
    if (filters.q?.trim()) params.set('q', filters.q.trim());
    if (filters.topics?.length) params.set('topics', filters.topics.join(','));
    if (filters.minUpvotes !== undefined) params.set('minUpvotes', String(filters.minUpvotes));
    if (filters.minComments !== undefined) params.set('minComments', String(filters.minComments));
    if (filters.minViews !== undefined) params.set('minViews', String(filters.minViews));
    if (filters.publishedFrom) params.set('publishedFrom', filters.publishedFrom);
    if (filters.publishedTo) params.set('publishedTo', filters.publishedTo);
    if (cursor) params.set('cursor', cursor);

    const response = await apiClient.get(`/products?${params.toString()}`);
    return response.data;
  },
  
  async toggleVote(productId: string, hasVoted: boolean, token: string) {
    const config = {
      headers: { Authorization: `Bearer ${token}` },
      data: { productId },
    };
    const response = hasVoted
      ? await apiClient.delete('/votes', config)
      : await apiClient.post('/votes', { productId }, config);
    return response.data;
  },

  async getMyVotes(token: string): Promise<string[]> {
    const response = await apiClient.get('/votes/me', {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  },

  async getTopics(): Promise<Topic[]> {
    const response = await apiClient.get('/topics');
    return response.data;
  },

  async getMakerProducts(makerId: string): Promise<ProductApiModel[]> {
    const response = await apiClient.get(`/products/maker/${makerId}`);
    return response.data;
  },

  async getUpvotedProducts(token: string): Promise<ProductApiModel[]> {
    const response = await apiClient.get('/products/upvoted', {
      headers: { Authorization: `Bearer ${token}` }
    });
    return response.data;
  }
};
