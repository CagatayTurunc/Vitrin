import { create } from 'zustand';
import axios from 'axios';
import { getErrorMessage } from '@/lib/errors';

export interface Notification {
  id: string;
  userId: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  notificationType?: string;
}

interface NotificationStore {
  notifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  error: string | null;
  fetchNotifications: (token: string) => Promise<void>;
  markAsRead: (notificationId: string, token: string) => Promise<void>;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const useNotificationStore = create<NotificationStore>((set) => ({
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  error: null,

  fetchNotifications: async (token: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await axios.get(`${API_URL}/api/notifications/me`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const notifications = response.data as Notification[];
      const unreadCount = notifications.filter((n: Notification) => !n.isRead).length;
      
      set({ notifications, unreadCount, isLoading: false });
    } catch (error: unknown) {
      set({ error: getErrorMessage(error, 'Bildirimler alınamadı.'), isLoading: false });
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
