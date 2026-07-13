"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Loader2, Plus, Bookmark, Check } from "lucide-react";
import { useSession } from "next-auth/react";
import { useToast } from "@/components/ui/use-toast";

interface AddToCollectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  productId: string;
}

export function AddToCollectionModal({ isOpen, onClose, productId }: AddToCollectionModalProps) {
  const { data: session } = useSession();
  const { toast } = useToast();
  
  const [collections, setCollections] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  
  const [newCollectionName, setNewCollectionName] = useState("");
  const [newCollectionDesc, setNewCollectionDesc] = useState("");
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    if (isOpen && session?.user) {
      fetchCollections();
    }
  }, [isOpen, session]);

  const fetchCollections = async () => {
    setIsLoading(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/collections/user/${(session?.user as any)?.id}`);
      if (res.ok) {
        const data = await res.json();
        setCollections(data);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateCollection = async (e?: React.FormEvent | React.MouseEvent) => {
    if (e) e.preventDefault();
    if (!newCollectionName.trim()) return;
    
    setIsCreating(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/collections", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId: (session?.user as any)?.id,
          name: newCollectionName,
          description: newCollectionDesc
        }),
      });
      
      if (res.ok) {
        setNewCollectionName("");
        setNewCollectionDesc("");
        await fetchCollections(); // Refresh the list
        toast({
          title: "Koleksiyon oluşturuldu",
          description: "Yeni koleksiyonunuz başarıyla oluşturuldu.",
        });
      } else {
        const errText = await res.text();
        console.error("API Error:", res.status, errText);
        toast({
          title: "API Hatası (" + res.status + ")",
          description: errText.substring(0, 100),
          variant: "destructive"
        });
      }
    } catch (e: any) {
      console.error(e);
      toast({
        title: "Bağlantı Hatası",
        description: e.message || "Koleksiyon oluşturulurken bir hata oluştu.",
        variant: "destructive"
      });
    } finally {
      setIsCreating(false);
    }
  };

  const handleAddToCollection = async (collectionId: string) => {
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/collections/${collectionId}/products/${productId}`, {
        method: "POST",
      });
      
      if (res.ok) {
        toast({
          title: "Eklendi",
          description: "Ürün koleksiyona başarıyla eklendi.",
        });
        onClose();
      } else {
        toast({
          title: "Hata",
          description: "Ürün zaten bu koleksiyonda olabilir.",
          variant: "destructive"
        });
      }
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Bookmark className="h-5 w-5 text-primary" />
            Koleksiyona Ekle
          </DialogTitle>
          <DialogDescription>
            Bu ürünü daha sonra kolayca bulmak için bir koleksiyona ekleyin.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 my-4">
          <div className="space-y-3 bg-muted/50 p-4 rounded-xl border border-border">
            <h4 className="text-sm font-semibold">Yeni Koleksiyon Oluştur</h4>
            <Input 
              placeholder="Koleksiyon Adı (Örn: En İyi Mobil Araçlar)" 
              value={newCollectionName}
              onChange={(e) => setNewCollectionName(e.target.value)}
              className="bg-background"
            />
            <div className="flex gap-2">
              <Input 
                placeholder="Kısa bir açıklama..." 
                value={newCollectionDesc}
                onChange={(e) => setNewCollectionDesc(e.target.value)}
                className="bg-background flex-1"
              />
              <Button type="button" onClick={handleCreateCollection} disabled={isCreating || !newCollectionName.trim()}>
                {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              </Button>
            </div>
          </div>

          <div>
            <h4 className="text-sm font-semibold mb-3">Mevcut Koleksiyonlarınız</h4>
            {isLoading ? (
              <div className="flex justify-center py-4">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : collections.length > 0 ? (
              <div className="space-y-2 max-h-[40vh] overflow-y-auto pr-2">
                {collections.map(c => (
                  <div key={c.id} className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors">
                    <div className="flex flex-col min-w-0 pr-4">
                      <span className="font-medium truncate">{c.name}</span>
                      <span className="text-xs text-muted-foreground truncate">{c.productCount} ürün</span>
                    </div>
                    <Button variant="secondary" size="sm" onClick={() => handleAddToCollection(c.id)}>
                      Ekle
                    </Button>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">Henüz bir koleksiyonunuz yok.</p>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
