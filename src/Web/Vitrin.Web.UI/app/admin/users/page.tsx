"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Shield, ShieldAlert, User as UserIcon } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { useSession } from "next-auth/react";

export default function AdminUsers() {
  const { data: session } = useSession();
  const [users, setUsers] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (session?.accessToken) {
      fetchUsers(session.accessToken as string);
    }
  }, [session?.accessToken]);

  const fetchUsers = async (token: string) => {
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/admin/users", {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setUsers(data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRoleChange = async (userId: string, newRole: number) => {
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/admin/users/${userId}/role`, {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${session?.accessToken}`
        },
        body: JSON.stringify(newRole)
      });
      
      if (res.ok) {
        setUsers(users.map(u => u.id === userId ? { ...u, role: newRole } : u));
      }
    } catch (err) {
      console.error(err);
    }
  };

  const getRoleBadge = (role: number) => {
    switch(role) {
      case 0: return <span className="inline-flex items-center gap-1 px-2 py-1 rounded-md bg-blue-100 text-blue-700 text-xs font-medium"><UserIcon className="h-3 w-3"/> Member</span>;
      case 1: return <span className="inline-flex items-center gap-1 px-2 py-1 rounded-md bg-purple-100 text-purple-700 text-xs font-medium"><Shield className="h-3 w-3"/> Maker</span>;
      case 2: return <span className="inline-flex items-center gap-1 px-2 py-1 rounded-md bg-red-100 text-red-700 text-xs font-medium"><ShieldAlert className="h-3 w-3"/> Admin</span>;
      default: return <span className="px-2 py-1 rounded-md bg-gray-100 text-gray-700 text-xs font-medium">Bilinmeyen</span>;
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Kullanıcı Yönetimi</h1>
        <p className="text-muted-foreground">Kayıtlı kullanıcıları görüntüleyin ve rollerini yönetin.</p>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Kullanıcı</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Kayıt Tarihi</TableHead>
              <TableHead>Mevcut Rol</TableHead>
              <TableHead className="text-right">Rol Değiştir</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24">Yükleniyor...</TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24 text-muted-foreground">Kullanıcı bulunamadı.</TableCell>
              </TableRow>
            ) : (
              users.map((user) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-3">
                      <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center">
                        <span className="text-xs font-bold text-muted-foreground">{user.fullName?.[0]?.toUpperCase() || user.username?.[0]?.toUpperCase()}</span>
                      </div>
                      <div>
                        <div>{user.fullName || "İsimsiz"}</div>
                        <div className="text-xs text-muted-foreground">@{user.username}</div>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>{new Date(user.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    {getRoleBadge(user.role)}
                  </TableCell>
                  <TableCell className="text-right">
                    <select 
                      className="text-sm border rounded-md p-1.5 bg-background focus:ring-2 focus:ring-ring outline-none"
                      value={user.role}
                      onChange={(e) => handleRoleChange(user.id, parseInt(e.target.value))}
                    >
                      <option value={0}>Member Yap</option>
                      <option value={1}>Maker Yap</option>
                      <option value={2}>Admin Yap</option>
                    </select>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
