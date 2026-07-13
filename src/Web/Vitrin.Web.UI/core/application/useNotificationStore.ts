import { create } from 'zustand';
import axios from 'axios';

export interface Notification {
  id: string;
  userId: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

interface NotificationStore {
  notifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  error: string | null;
  fetchNotifications: (token: string, userId: string) => Promise<void>;
  markAsRead: (notificationId: string, token: string) => Promise<void>;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5177';

export const useNotificationStore = create<NotificationStore>((set, get) => ({
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  error: null,

  fetchNotifications: async (token: string, userId: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await axios.get(`${API_URL}/api/notifications/${userId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const notifications = response.data;
      const unreadCount = notifications.filter((n: Notification) => !n.isRead).length;
      
      set({ notifications, unreadCount, isLoading: false });
    } catch (error: any) {
      set({ error: error.message || 'Bildirimler alınamadı', isLoading: false });
    }
  },

  markAsRead: async (notificationId: string, token: string) => {
    try {
      // Optimistic UI Update
      set((state) => {
        const notifications = state.notifications.map(n => 
          n.id === notificationId ? { ...n, isRead: true } : n
        );
        return {
          notifications,
          unreadCount: notifications.filter((n: Notification) => !n.isRead).length
        };
      });

      await axios.post(`${API_URL}/api/notifications/${notificationId}/read`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      });
    } catch (error) {
      console.error('Bildirim okundu işaretlenemedi', error);
      // Revert if needed
    }
  }
}));
