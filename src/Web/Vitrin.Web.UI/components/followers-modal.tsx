"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import Image from "next/image";
import Link from "next/link";
import { User as UserIcon } from "lucide-react";

export function FollowersModal({ 
  isOpen, 
  onClose, 
  username, 
  type 
}: { 
  isOpen: boolean; 
  onClose: () => void; 
  username: string;
  type: 'followers' | 'following';
}) {
  const [users, setUsers] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen && username) {
      fetchUsers();
    }
  }, [isOpen, username, type]);

  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${username}/${type}`);
      if (res.ok) {
        setUsers(await res.json());
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{type === 'followers' ? 'Takipçiler' : 'Takip Edilenler'}</DialogTitle>
          <DialogDescription>
            {users.length} kişi listeleniyor
          </DialogDescription>
        </DialogHeader>
        
        <div className="max-h-[60vh] overflow-y-auto space-y-4 pr-2">
          {isLoading ? (
            <div className="py-4 text-center text-muted-foreground text-sm">Yükleniyor...</div>
          ) : users.length === 0 ? (
            <div className="py-4 text-center text-muted-foreground text-sm">Kimse bulunamadı.</div>
          ) : (
            users.map((u) => (
              <div key={u.id} className="flex items-center gap-3 p-2 rounded-xl hover:bg-muted/50 transition-colors">
                <div className="h-10 w-10 shrink-0 rounded-full bg-gradient-to-tr from-purple-500 to-blue-500 flex items-center justify-center text-white overflow-hidden shadow-inner">
                  {u.avatarUrl ? (
                    <Image src={u.avatarUrl} alt={u.fullName || u.username} width={40} height={40} className="object-cover w-full h-full" />
                  ) : (
                    u.fullName?.charAt(0) || u.username?.charAt(0) || <UserIcon className="h-5 w-5" />
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <Link href={`/profile/${u.username}`} onClick={onClose} className="block truncate font-semibold hover:underline">
                    {u.fullName || u.username}
                  </Link>
                  <div className="truncate text-xs text-muted-foreground">@{u.username}</div>
                </div>
              </div>
            ))
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
