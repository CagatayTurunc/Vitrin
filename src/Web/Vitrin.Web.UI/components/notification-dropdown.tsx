"use client";

import { useEffect, useState } from 'react';
import { Bell } from 'lucide-react';
import { useSession } from 'next-auth/react';
import { useNotificationStore } from '@/core/application/useNotificationStore';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';

export function NotificationDropdown() {
  const { data: session } = useSession();
  const [isOpen, setIsOpen] = useState(false);
  const { notifications, unreadCount, fetchNotifications, markAsRead } = useNotificationStore();

  useEffect(() => {
    if (session?.user?.id && session.accessToken) {
      fetchNotifications(session.accessToken, session.user.id);
      
      // Optionally set up polling here for real-time updates
      const intervalId = setInterval(() => {
        fetchNotifications(session.accessToken, session.user.id);
      }, 15000); // Check every 15 seconds

      return () => clearInterval(intervalId);
    }
  }, [session, fetchNotifications]);

  const handleOpenChange = (open: boolean) => {
    setIsOpen(open);
    if (open) {
      // Mark all as read when opened? Let's just mark individually if clicked, or mark all as read.
      // For simplicity, we can let user click them, or if we want to mark all as read:
    }
  };

  const handleNotificationClick = async (id: string, isRead: boolean) => {
    if (!isRead && session?.accessToken) {
      await markAsRead(id, session.accessToken);
    }
  };

  if (!session) return null;

  return (
    <Popover open={isOpen} onOpenChange={handleOpenChange}>
      <PopoverTrigger className="relative inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground h-10 w-10">
        <Bell className="h-5 w-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground ring-2 ring-background">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </PopoverTrigger>
      <PopoverContent className="w-80 p-0" align="end">
        <div className="flex items-center justify-between px-4 py-3 border-b border-border">
          <h4 className="font-semibold text-sm">Bildirimler</h4>
          {unreadCount > 0 && (
            <span className="text-xs text-muted-foreground">{unreadCount} okunmamış</span>
          )}
        </div>
        <div className="max-h-[300px] overflow-y-auto">
          {notifications.length === 0 ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              Henüz bir bildiriminiz yok.
            </div>
          ) : (
            <div className="flex flex-col">
              {notifications.map((notification) => (
                <button
                  key={notification.id}
                  onClick={() => handleNotificationClick(notification.id, notification.isRead)}
                  className={`flex flex-col items-start px-4 py-3 text-left transition-colors hover:bg-muted/50 ${
                    !notification.isRead ? 'bg-primary/5' : ''
                  }`}
                >
                  <p className={`text-sm ${!notification.isRead ? 'font-medium text-foreground' : 'text-muted-foreground'}`}>
                    {notification.message}
                  </p>
                  <span className="text-xs text-muted-foreground mt-1">
                    {formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true, locale: tr })}
                  </span>
                </button>
              ))}
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
