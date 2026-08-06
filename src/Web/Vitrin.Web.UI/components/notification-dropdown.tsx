"use client";

import { useEffect, useState } from 'react';
import { Bell, CheckCircle2, XCircle, MessageSquare, UserCheck, Heart, Megaphone, AtSign, SmilePlus, Ban, Scale, SearchCheck, Radio } from 'lucide-react';
import { useSession } from 'next-auth/react';
import { useNotificationStore } from '@/core/application/useNotificationStore';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';
import Link from 'next/link';

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
    case 'saved_search_match':
      return <SearchCheck className="w-4 h-4 text-cyan-500 shrink-0 mt-0.5" />;
    case 'topic_product_published':
      return <Radio className="w-4 h-4 text-violet-500 shrink-0 mt-0.5" />;
    default:
      return <Megaphone className="w-4 h-4 text-muted-foreground shrink-0 mt-0.5" />;
  }
}

export function NotificationDropdown() {
  const { data: session } = useSession();
  const [isOpen, setIsOpen] = useState(false);
  const {
    notifications,
    unreadCount,
    realtimeStatus,
    fetchNotifications,
    markAsRead,
    markAllAsRead,
    connectRealtime,
    disconnectRealtime,
  } = useNotificationStore();
  const accessToken = session?.accessToken;

  useEffect(() => {
    if (accessToken) {
      void fetchNotifications(accessToken);
      connectRealtime(accessToken);
      return disconnectRealtime;
    }
  }, [accessToken, connectRealtime, disconnectRealtime, fetchNotifications]);

  const handleOpenChange = (open: boolean) => {
    setIsOpen(open);
  };

  const handleNotificationClick = async (id: string, isRead: boolean) => {
    if (!isRead && session?.accessToken) {
      await markAsRead(id, session.accessToken);
    }
  };

  const handleMarkAllAsRead = async () => {
    if (session?.accessToken) {
      await markAllAsRead(session.accessToken);
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
          <div className="flex items-center gap-2">
            <h4 className="font-semibold text-sm">Bildirimler</h4>
            <span
              className={`h-2 w-2 rounded-full ${realtimeStatus === 'connected' ? 'bg-emerald-500' : 'bg-amber-500'}`}
              title={realtimeStatus === 'connected' ? 'Canlı bağlantı aktif' : 'Canlı bağlantı kuruluyor'}
            />
          </div>
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
              title="Tümünü okundu işaretle"
            >
              Tümünü okundu işaretle
            </button>
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
        <div className="border-t border-border p-2">
          <Link href="/notifications" className="block rounded-lg px-3 py-2 text-center text-sm font-bold text-primary hover:bg-muted">Tüm bildirimleri aç</Link>
        </div>
      </PopoverContent>
    </Popover>
  );
}
