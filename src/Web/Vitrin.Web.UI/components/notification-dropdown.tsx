"use client";

import { useEffect, useState } from 'react';
import { Bell, CheckCircle2, XCircle, MessageSquare, UserCheck, Heart, Megaphone, AtSign, SmilePlus, Ban, Scale } from 'lucide-react';
import { useSession } from 'next-auth/react';
import { useNotificationStore } from '@/core/application/useNotificationStore';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';

type NotificationType = 'product_approved' | 'product_rejected' | 'comment' | 'follow' | 'upvote' | 'maker_approved' | string | undefined;

function NotificationIcon({ type }: { type: NotificationType }) {
  switch (type) {
    case 'product_approved':
      return <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />;
    case 'product_rejected':
      return <XCircle className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />;
    case 'comment':
    case 'comment_reply':
    case 'comment_on_product':
      return <MessageSquare className="w-4 h-4 text-blue-500 shrink-0 mt-0.5" />;
    case 'comment_mention':
      return <AtSign className="w-4 h-4 text-cyan-500 shrink-0 mt-0.5" />;
    case 'comment_reaction':
      return <SmilePlus className="w-4 h-4 text-pink-500 shrink-0 mt-0.5" />;
    case 'follow':
      return <UserCheck className="w-4 h-4 text-purple-500 shrink-0 mt-0.5" />;
    case 'upvote':
      return <Heart className="w-4 h-4 text-pink-500 shrink-0 mt-0.5" />;
    case 'maker_approved':
      return <UserCheck className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />;
    case 'account_banned':
      return <Ban className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />;
    case 'appeal_approved':
    case 'account_ban_revoked':
      return <Scale className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />;
    case 'appeal_rejected':
      return <Scale className="w-4 h-4 text-amber-500 shrink-0 mt-0.5" />;
    default:
      return <Megaphone className="w-4 h-4 text-muted-foreground shrink-0 mt-0.5" />;
  }
}

export function NotificationDropdown() {
  const { data: session } = useSession();
  const [isOpen, setIsOpen] = useState(false);
  const { notifications, unreadCount, fetchNotifications, markAsRead } = useNotificationStore();
  const accessToken = session?.accessToken;

  useEffect(() => {
    if (accessToken) {
      fetchNotifications(accessToken);
      const intervalId = setInterval(() => {
        fetchNotifications(accessToken);
      }, 15000);
      return () => clearInterval(intervalId);
    }
  }, [accessToken, fetchNotifications]);

  const handleOpenChange = (open: boolean) => {
    setIsOpen(open);
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
        <div className="max-h-[380px] overflow-y-auto">
          {notifications.length === 0 ? (
            <div className="p-6 text-center text-sm text-muted-foreground">
              Henüz bir bildiriminiz yok.
            </div>
          ) : (
            <div className="flex flex-col divide-y divide-border/50">
              {notifications.map((notification) => (
                <button
                  key={notification.id}
                  onClick={() => handleNotificationClick(notification.id, notification.isRead)}
                  className={`flex items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50 w-full ${
                    !notification.isRead ? 'bg-primary/5' : ''
                  }`}
                >
                  <NotificationIcon type={notification.notificationType} />
                  <div className="flex-1 min-w-0">
                    <p className={`text-sm leading-snug ${!notification.isRead ? 'font-medium text-foreground' : 'text-muted-foreground'}`}>
                      {notification.message}
                    </p>
                    <span className="text-xs text-muted-foreground mt-1 block">
                      {formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true, locale: tr })}
                    </span>
                  </div>
                  {!notification.isRead && (
                    <span className="w-2 h-2 rounded-full bg-primary shrink-0 mt-1.5" />
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
