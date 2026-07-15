"use client";

import { useEffect, useState } from "react";
import { Check, X, ExternalLink } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { useSession } from "next-auth/react";

interface MakerRequest {
  id: string;
  fullName?: string | null;
  user: string;
  portfolioUrl: string;
  reason: string;
  createdAt: string;
}

export default function AdminMakerRequests() {
  const { data: session } = useSession();
  const [requests, setRequests] = useState<MakerRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = session?.accessToken;
    if (!token) return;

    const fetchRequests = async () => {
      try {
        const response = await fetch(
          process.env.NEXT_PUBLIC_API_URL + "/api/auth/admin/maker-applications",
          { headers: { Authorization: `Bearer ${token}` } },
        );
        if (response.ok) setRequests(await response.json() as MakerRequest[]);
      } catch (error) {
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    };

    void fetchRequests();
  }, [session?.accessToken]);

  const handleAction = async (id: string, action: "approve" | "reject") => {
    if (!session?.accessToken) return;

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/admin/maker-applications/${id}/${action}`, {
        method: "POST",
        headers: { Authorization: `Bearer ${session?.accessToken}` }
      });
      if (res.ok) {
        setRequests((current) => current.filter((request) => request.id !== id));
      }
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Maker Başvuruları</h1>
        <p className="text-muted-foreground">Kullanıcıların Maker olmak için gönderdiği onay bekleyen başvurular.</p>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Kullanıcı</TableHead>
              <TableHead>Portfolyo / LinkedIn</TableHead>
              <TableHead>Başvuru Nedeni</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead className="text-right">Aksiyonlar</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24">Yükleniyor...</TableCell>
              </TableRow>
            ) : requests.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24 text-muted-foreground">Bekleyen başvuru bulunmuyor.</TableCell>
              </TableRow>
            ) : (
              requests.map((req) => (
                <TableRow key={req.id}>
                  <TableCell className="font-medium">
                    <div>{req.fullName || "İsimsiz"}</div>
                    <div className="text-xs text-muted-foreground">{req.user}</div>
                  </TableCell>
                  <TableCell>
                    <a href={req.portfolioUrl} target="_blank" rel="noreferrer" className="text-blue-500 hover:underline flex items-center gap-1">
                      İncele <ExternalLink className="h-3 w-3" />
                    </a>
                  </TableCell>
                  <TableCell className="max-w-xs truncate" title={req.reason}>
                    {req.reason}
                  </TableCell>
                  <TableCell>{new Date(req.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-2">
                      <button 
                        onClick={() => handleAction(req.id, "approve")}
                        className="p-2 bg-green-100 text-green-700 rounded-md hover:bg-green-200" title="Onayla"
                      >
                        <Check className="h-4 w-4" />
                      </button>
                      <button 
                        onClick={() => handleAction(req.id, "reject")}
                        className="p-2 bg-red-100 text-red-700 rounded-md hover:bg-red-200" title="Reddet"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>
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
