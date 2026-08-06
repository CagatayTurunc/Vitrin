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
  realtimeStatus: 'disconnected' | 'connecting' | 'connected';
  fetchNotifications: (token: string) => Promise<void>;
  markAsRead: (notificationId: string, token: string) => Promise<void>;
  markAllAsRead: (token: string) => Promise<void>;
  connectRealtime: (token: string) => void;
  disconnectRealtime: () => void;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
let streamController: AbortController | null = null;
let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

function parseSseFrame(frame: string): Notification | null {
  const lines = frame.split('\n');
  const eventName = lines
    .find((line) => line.startsWith('event:'))
    ?.slice('event:'.length)
    .trim();

  if (eventName !== 'notification') return null;

  const data = lines
    .filter((line) => line.startsWith('data:'))
    .map((line) => line.slice('data:'.length).trimStart())
    .join('\n');

  if (!data) return null;

  try {
    return JSON.parse(data) as Notification;
  } catch {
    return null;
  }
}

export const useNotificationStore = create<NotificationStore>((set, get) => ({
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  error: null,
  realtimeStatus: 'disconnected',

  fetchNotifications: async (token: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await axios.get(`${API_URL}/api/notifications/me`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const notifications = response.data as Notification[];
      const unreadCount = notifications.filter((notification) => !notification.isRead).length;

      set({ notifications, unreadCount, isLoading: false });
    } catch (error: unknown) {
      set({ error: getErrorMessage(error, 'Bildirimler alınamadı.'), isLoading: false });
    }
  },

  markAsRead: async (notificationId: string, token: string) => {
    try {
      set((state) => {
        const notifications = state.notifications.map((notification) =>
          notification.id === notificationId ? { ...notification, isRead: true } : notification
        );
        return {
          notifications,
          unreadCount: notifications.filter((notification) => !notification.isRead).length
        };
      });

      await axios.post(`${API_URL}/api/notifications/${notificationId}/read`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      });
    } catch (error) {
      console.error('Bildirim okundu işaretlenemedi', error);
    }
  },

  markAllAsRead: async (token: string) => {
    try {
      set((state) => {
        const notifications = state.notifications.map((n) => ({ ...n, isRead: true }));
        return { notifications, unreadCount: 0 };
      });

      await axios.post(`${API_URL}/api/notifications/read-all`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      });
    } catch (error) {
      console.error('Tüm bildirimler okundu işaretlenemedi', error);
    }
  },

  connectRealtime: (token: string) => {
    get().disconnectRealtime();

    const controller = new AbortController();
    streamController = controller;

    const consume = async () => {
      if (controller.signal.aborted || streamController !== controller) return;

      set({ realtimeStatus: 'connecting' });
      let shouldReconnect = true;

      try {
        const response = await fetch(`${API_URL}/api/notifications/stream`, {
          headers: {
            Accept: 'text/event-stream',
            Authorization: `Bearer ${token}`,
          },
          cache: 'no-store',
          signal: controller.signal,
        });

        if (response.status === 204 || response.status === 401 || response.status === 403) {
          shouldReconnect = false;
          set({ realtimeStatus: 'disconnected' });
          return;
        }

        if (!response.ok || !response.body) {
          throw new Error(`SSE bağlantısı kurulamadı (${response.status}).`);
        }

        set({ realtimeStatus: 'connected', error: null });

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (!controller.signal.aborted) {
          const { value, done } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n');
          const frames = buffer.split('\n\n');
          buffer = frames.pop() ?? '';

          for (const frame of frames) {
            const notification = parseSseFrame(frame);
            if (!notification) continue;

            set((state) => {
              if (state.notifications.some((item) => item.id === notification.id)) {
                return state;
              }

              const notifications = [notification, ...state.notifications].slice(0, 50);
              return {
                notifications,
                unreadCount: notifications.filter((item) => !item.isRead).length,
              };
            });
          }
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          console.warn('Gerçek zamanlı bildirim bağlantısı yenilenecek.', error);
        }
      } finally {
        if (shouldReconnect && !controller.signal.aborted && streamController === controller) {
          set({ realtimeStatus: 'connecting' });
          reconnectTimer = setTimeout(() => void consume(), 3000);
        }
      }
    };

    void consume();
  },

  disconnectRealtime: () => {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }

    streamController?.abort();
    streamController = null;
    set({ realtimeStatus: 'disconnected' });
  }
}));
