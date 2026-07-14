"use client";

import { useCallback, useState, useEffect, use, useRef } from "react";
import { useSession } from "next-auth/react";
import Image from "next/image";
import Link from "next/link";
import { Button, buttonVariants } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { ArrowUp, ExternalLink, MessageSquare, Share2, AlertCircle, Bookmark, X, ChevronLeft, ChevronRight, Maximize2, Star } from "lucide-react";
import { LoginModal } from "@/components/login-modal";
import { AddToCollectionModal } from "@/components/add-to-collection-modal";
import dynamic from "next/dynamic";
import "@uiw/react-markdown-preview/markdown.css";
import type { ProductDetailApiModel } from "@/core/domain/product.types";
import type { UserProfile } from "@/core/domain/user.types";

interface ProductComment {
  id: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
  parentCommentId?: string | null;
  isDeleted: boolean;
  updatedAt?: string | null;
}

interface ProductCommentNode extends ProductComment {
  replies: ProductCommentNode[];
}

const MarkdownPreview = dynamic(() => import("@uiw/react-markdown-preview").then((mod) => mod.default), { ssr: false });

export default function ProductDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const unwrappedParams = use(params);
  const slug = unwrappedParams.slug as string;
  const { data: session } = useSession();

  const [product, setProduct] = useState<ProductDetailApiModel | null>(null);
  const [maker, setMaker] = useState<UserProfile | null>(null);
  const [comments, setComments] = useState<ProductComment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false);
  const [newComment, setNewComment] = useState("");
  const [isSubmittingComment, setIsSubmittingComment] = useState(false);
  const [replyToId, setReplyToId] = useState<string | null>(null);
  const [replyContent, setReplyContent] = useState("");
  const [editCommentId, setEditCommentId] = useState<string | null>(null);
  const [editContent, setEditContent] = useState("");
  const [isCollectionModalOpen, setIsCollectionModalOpen] = useState(false);
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const galleryRef = useRef<HTMLDivElement>(null);

  const [aiSummary, setAiSummary] = useState<string | null>(null);
  const [aiTags, setAiTags] = useState<string[]>([]);
  const [isGeneratingAi, setIsGeneratingAi] = useState(false);
  const [recommendations, setRecommendations] = useState<ProductDetailApiModel[]>([]);

  const scrollGallery = (direction: 'left' | 'right') => {
    if (galleryRef.current) {
      const scrollAmount = galleryRef.current.clientWidth * 0.75;
      galleryRef.current.scrollBy({ left: direction === 'left' ? -scrollAmount : scrollAmount, behavior: 'smooth' });
    }
  };

  const fetchAiData = useCallback(async (productId: string) => {
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/ai/product/${productId}`);
      if (res.ok) {
        const data = await res.json();
        setAiSummary(data.summary);
        const tagsArray = data.tags ? data.tags.split(',').map((t: string) => t.trim()) : [];
        setAiTags(tagsArray);
        
        // Fetch recommendations
        const recRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/ai/product/${productId}/recommendations`);
        if (recRes.ok) {
          const recIds = await recRes.json();
          if (recIds && recIds.length > 0) {
            const batchRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/batch?ids=${recIds.join(',')}`);
            if (batchRes.ok) {
              setRecommendations(await batchRes.json() as ProductDetailApiModel[]);
            }
          }
        }
      }
    } catch (error) {
      console.error("AI veri çekme hatası:", error);
    }
  }, []);

  const handleGenerateAi = async () => {
    if (!product) return;
    setIsGeneratingAi(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/ai/analyze`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          productId: product.id,
          productName: product.name,
          productDescription: product.description || product.tagline || ""
        })
      });
      if (res.ok) {
        await fetchAiData(product.id);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsGeneratingAi(false);
    }
  };

  const fetchProductData = useCallback(async () => {
    try {
      // 1. Fetch Product Details
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/${slug}`);
      if (res.ok) {
        const productData = await res.json() as ProductDetailApiModel;
        setProduct(productData);

        // Fetch AI Data
        void fetchAiData(productData.id);

        // 2. Fetch Comments
        const commentsRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${productData.id}`);
        if (commentsRes.ok) {
          const commentsData = await commentsRes.json() as ProductComment[];
          setComments(commentsData);
        }

        // 3. Fetch Maker
        if (productData.makerId) {
          const makerRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${productData.makerId}`);
          if (makerRes.ok) {
            const makerData = await makerRes.json() as UserProfile;
            setMaker(makerData);
          }
        }
      } else {
        setProduct(null);
      }
    } catch (error) {
      console.error(error);
      setProduct(null);
    } finally {
      setIsLoading(false);
    }
  }, [fetchAiData, slug]);

  useEffect(() => {
    // Client-side route data is synchronized after the network request resolves.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    if (slug) void fetchProductData();
  }, [fetchProductData, slug]);

  const handleFollowMaker = async () => {
    if (!session?.accessToken) {
      setIsLoginModalOpen(true);
      return;
    }

    if (!maker) return;

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${maker.username}/follow`, {
        method: "POST",
        headers: { Authorization: `Bearer ${session.accessToken}` }
      });
      if (res.ok) {
        // Optimistic update
        setMaker((current) => current ? {
          ...current,
          isFollowing: !current.isFollowing,
          followerCount: current.isFollowing
            ? current.followerCount - 1
            : current.followerCount + 1,
        } : current);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleUpvote = async () => {
    if (!session?.accessToken) {
      setIsLoginModalOpen(true);
      return;
    }
    if (!product) return;

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/${product.id}/vote`, {
        method: "POST",
        headers: { Authorization: `Bearer ${session.accessToken}` }
      });
      if (res.ok) {
        const data = await res.json() as { upvotes: number };
        setProduct({ ...product, upvotes: data.upvotes });
        
        // Gamification: Record vote activity for streaks and badges
        await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/me/record-vote`, {
          method: "POST",
          headers: { Authorization: `Bearer ${session.accessToken}` }
        }).catch(e => console.error("Streak could not be updated", e));
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleCommentSubmit = async (parentId?: string) => {
    if (!session?.accessToken) {
      setIsLoginModalOpen(true);
      return;
    }
    if (!product) return;
    const contentToSend = parentId ? replyContent : newComment;
    if (!contentToSend.trim()) return;

    setIsSubmittingComment(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/comments", {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${session.accessToken}` 
        },
        body: JSON.stringify({
          productId: product.id,
          userId: session.user.id,
          userName: session.user.username || session.user.fullName || session.user.name || "Kullanıcı",
          content: contentToSend,
          parentCommentId: parentId || null
        })
      });
      
      if (res.ok) {
        if (parentId) {
          setReplyContent("");
          setReplyToId(null);
        } else {
          setNewComment("");
        }
        
        // Refresh comments
        const commentsRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${product.id}`);
        if (commentsRes.ok) {
          setComments(await commentsRes.json() as ProductComment[]);
        }
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmittingComment(false);
    }
  };

  const handleCommentEdit = async (commentId: string) => {
    if (!editContent.trim() || !product) return;
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${commentId}`, {
        method: "PUT",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${session?.accessToken}` 
        },
        body: JSON.stringify({ content: editContent })
      });
      if (res.ok) {
        setEditCommentId(null);
        // Refresh comments
        const commentsRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${product.id}`);
        if (commentsRes.ok) setComments(await commentsRes.json() as ProductComment[]);
      }
    } catch (err) { console.error(err); }
  };

  const handleCommentDelete = async (commentId: string) => {
    if (!product || !confirm("Bu yorumu silmek istediğinize emin misiniz?")) return;
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${commentId}`, {
        method: "DELETE",
        headers: { "Authorization": `Bearer ${session?.accessToken}` }
      });
      if (res.ok) {
        const commentsRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/comments/${product.id}`);
        if (commentsRes.ok) setComments(await commentsRes.json() as ProductComment[]);
      }
    } catch (err) { console.error(err); }
  };

  // Build hierarchical comments
  const buildCommentTree = () => {
    const map = new Map<string, ProductCommentNode>();
    comments.forEach((comment) => map.set(comment.id, { ...comment, replies: [] }));
    const roots: ProductCommentNode[] = [];
    
    comments.forEach(c => {
      if (c.parentCommentId && map.has(c.parentCommentId)) {
        const parent = map.get(c.parentCommentId);
        const child = map.get(c.id);
        if (parent && child) parent.replies.push(child);
      } else {
        const root = map.get(c.id);
        if (root) roots.push(root);
      }
    });

    // Sort replies oldest first (chronological order)
    map.forEach(c => {
      c.replies.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    });

    return roots;
  };

  if (isLoading) {
    return <div className="flex h-[50vh] items-center justify-center">Yükleniyor...</div>;
  }

  if (!product) {
    return (
      <div className="flex flex-col items-center justify-center h-[50vh] space-y-4">
        <AlertCircle className="h-12 w-12 text-muted-foreground" />
        <h2 className="text-2xl font-bold">Ürün Bulunamadı</h2>
        <p className="text-muted-foreground">Aradığınız ürün yayından kaldırılmış veya hiç var olmamış olabilir.</p>
        <Link href="/" className={buttonVariants()}>Anasayfaya Dön</Link>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-10 pb-20">
      {/* Product Header */}
      <div className="flex flex-col md:flex-row gap-6 items-start justify-between">
        <div className="flex gap-6 items-center">
          <div className="h-24 w-24 rounded-2xl bg-muted flex-shrink-0 flex items-center justify-center shadow-sm overflow-hidden border">
             {product.thumbnailUrl ? (
               <Image src={product.thumbnailUrl} alt={product.name} width={96} height={96} className="object-cover" />
             ) : (
               <span className="text-3xl font-bold text-muted-foreground">{product.name.substring(0, 2).toUpperCase()}</span>
             )}
          </div>
          <div>
            <h1 className="text-4xl font-extrabold tracking-tight mb-2">{product.name}</h1>
            <h2 className="text-xl text-muted-foreground font-medium">{product.tagline}</h2>
            <div className="flex flex-wrap gap-2 mt-4">
              {product.topics?.map((topic) => (
                <span key={topic.id} className="text-xs font-medium bg-secondary text-secondary-foreground px-2.5 py-1 rounded-full">
                  {topic.name}
                </span>
              ))}
            </div>
          </div>
        </div>
        
        <div className="flex flex-col md:flex-row gap-3 w-full md:w-auto">
          <Button size="lg" variant="outline" className="gap-2 h-14 px-6 rounded-xl font-semibold w-full md:w-auto">
            Geliştirici Sitesi <ExternalLink className="h-4 w-4" />
          </Button>
          <Button 
            size="lg" 
            className="gap-2 h-14 px-8 rounded-xl font-bold w-full md:w-auto bg-[#00A170] hover:bg-[#008f63] text-white shadow-md transition-all hover:scale-105 active:scale-95"
            onClick={handleUpvote}
          >
            <ArrowUp className="h-5 w-5" /> UPVOTE {product.upvotes > 0 && <span className="opacity-90 ml-1">({product.upvotes})</span>}
          </Button>
        </div>
      </div>

      {/* Main Content & Sidebar */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
        
        {/* Left Column (Main) */}
        <div className="lg:col-span-2 space-y-10">
          
          {/* AI Summary Section */}
          <div className="bg-gradient-to-r from-purple-500/10 via-fuchsia-500/5 to-transparent rounded-2xl border border-purple-500/20 p-6 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-10">
              <Star className="w-24 h-24 text-purple-600" />
            </div>
            
            <div className="relative z-10">
              <div className="flex items-center gap-2 mb-3 text-purple-600 dark:text-purple-400 font-semibold">
                <span className="text-xl">✨</span> AI Özeti
              </div>
              
              {aiSummary ? (
                <div className="space-y-4">
                  <p className="text-lg font-medium leading-relaxed text-foreground/90">
                    &ldquo;{aiSummary}&rdquo;
                  </p>
                  {aiTags.length > 0 && (
                    <div className="flex gap-2">
                      {aiTags.map(tag => (
                        <span key={tag} className="px-2.5 py-1 rounded-md bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 text-xs font-semibold">
                          #{tag}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              ) : (
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                  <p className="text-sm text-muted-foreground max-w-md">
                    Bu ürün için henüz yapay zeka destekli bir özet çıkarılmamış. Şimdi oluşturarak topluluğa yardımcı ol!
                  </p>
                  <Button 
                    onClick={handleGenerateAi} 
                    disabled={isGeneratingAi}
                    className="bg-purple-600 hover:bg-purple-700 text-white rounded-xl shadow-md border-0 gap-2 shrink-0"
                  >
                    {isGeneratingAi ? (
                      <span className="animate-pulse">✨ Düşünüyor...</span>
                    ) : (
                      <>✨ AI Özeti Çıkar</>
                    )}
                  </Button>
                </div>
              )}
            </div>
          </div>

          {/* Media Gallery */}
          {product.galleryUrls && product.galleryUrls.length > 0 ? (
            <div className="relative group">
              <div 
                ref={galleryRef}
                className="flex gap-4 overflow-x-auto pb-4 no-scrollbar snap-x scroll-smooth"
              >
                {product.galleryUrls.map((url: string, i: number) => (
                  <div 
                    key={i} 
                    className="relative aspect-video w-[85%] sm:w-[65%] md:w-[50%] flex-shrink-0 rounded-2xl overflow-hidden border shadow-sm snap-center cursor-pointer group/item"
                    onClick={() => setSelectedImage(url)}
                  >
                    <Image src={url} alt={`Gallery Image ${i + 1}`} fill sizes="(max-width: 640px) 85vw, (max-width: 768px) 65vw, 50vw" className="object-cover transition-transform duration-500 group-hover/item:scale-105" />
                    <div className="absolute inset-0 bg-black/0 group-hover/item:bg-black/20 transition-colors flex items-center justify-center">
                      <Maximize2 className="text-white opacity-0 group-hover/item:opacity-100 transition-opacity w-8 h-8 drop-shadow-md" />
                    </div>
                  </div>
                ))}
              </div>
              
              {product.galleryUrls.length > 1 && (
                <>
                  <button 
                    onClick={(e) => { e.preventDefault(); scrollGallery('left'); }}
                    className="absolute left-2 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-background/80 backdrop-blur border shadow-md flex items-center justify-center text-foreground opacity-0 group-hover:opacity-100 transition-opacity hover:bg-background z-10 hidden sm:flex"
                  >
                    <ChevronLeft className="w-5 h-5" />
                  </button>
                  <button 
                    onClick={(e) => { e.preventDefault(); scrollGallery('right'); }}
                    className="absolute right-2 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-background/80 backdrop-blur border shadow-md flex items-center justify-center text-foreground opacity-0 group-hover:opacity-100 transition-opacity hover:bg-background z-10 hidden sm:flex"
                  >
                    <ChevronRight className="w-5 h-5" />
                  </button>
                </>
              )}
            </div>
          ) : (
            <div className="aspect-video w-full rounded-2xl bg-gradient-to-br from-muted/50 to-muted border shadow-inner flex items-center justify-center text-muted-foreground/50">
              <span className="text-lg font-medium">Görsel Bulunmuyor</span>
            </div>
          )}

          <div className="prose prose-neutral dark:prose-invert max-w-none" data-color-mode="light">
            <h3 className="text-2xl font-bold mb-4">Ürün Hikayesi</h3>
            {product.description ? (
              <MarkdownPreview 
                source={product.description} 
                style={{ backgroundColor: 'transparent', color: 'inherit' }}
              />
            ) : (
              <p className="text-lg leading-relaxed text-muted-foreground">
                Bu ürün hakkında henüz detaylı bir hikaye girilmemiş.
              </p>
            )}
          </div>

          <hr className="border-muted/50" />

          {/* Comments Section */}
          <div id="comments" className="space-y-6">
            <h3 className="text-2xl font-bold flex items-center gap-2">
              <MessageSquare className="h-6 w-6" /> Tartışma ({comments.length})
            </h3>
            
            {/* Comment Input */}
            <div className="flex gap-4 p-5 rounded-2xl border bg-card shadow-sm">
              <div className="h-10 w-10 rounded-full bg-secondary flex-shrink-0 flex items-center justify-center">
                <span className="text-sm font-bold">{session?.user?.name?.[0]?.toUpperCase() || "A"}</span>
              </div>
              <div className="flex-1 space-y-3">
                <Textarea 
                  placeholder={session ? "Ne düşünüyorsun? Deneyimlerini paylaş..." : "Yorum yapmak için giriş yapmalısın."}
                  className="min-h-[100px] resize-none bg-background focus-visible:ring-1"
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  disabled={!session}
                />
                <div className="flex justify-end">
                  <Button 
                    onClick={session ? () => handleCommentSubmit() : () => setIsLoginModalOpen(true)}
                    disabled={Boolean(session) && (!newComment.trim() || isSubmittingComment)}
                    className="rounded-xl px-6 font-medium"
                  >
                    {isSubmittingComment ? "Gönderiliyor..." : "Yorumu Gönder"}
                  </Button>
                </div>
              </div>
            </div>

            {/* Comments List */}
            <div className="space-y-6 mt-8">
              {comments.length === 0 ? (
                <div className="text-center py-10 text-muted-foreground bg-muted/20 rounded-2xl border border-dashed">
                  İlk yorumu sen yap! Bu ürün hakkında ne düşünüyorsun?
                </div>
              ) : (
                buildCommentTree().map((comment) => (
                  <div key={comment.id} className="space-y-4">
                    <div className={`flex gap-4 p-5 rounded-2xl transition-colors ${comment.userId === product.makerId ? 'bg-primary/5 border border-primary/20' : 'bg-card border shadow-sm'} ${comment.isDeleted ? 'opacity-60' : ''}`}>
                      <div className={`h-10 w-10 rounded-full flex-shrink-0 flex items-center justify-center font-bold text-sm text-white ${comment.userId === product.makerId ? 'bg-primary' : 'bg-secondary text-secondary-foreground'}`}>
                        {comment.isDeleted ? "-" : (comment.userName?.[0]?.toUpperCase() || "U")}
                      </div>
                      <div className="flex-1 space-y-1">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold">{comment.isDeleted ? "[Silinmiş Kullanıcı]" : comment.userName}</span>
                            {!comment.isDeleted && comment.userId === product.makerId && (
                              <span className="text-[10px] font-bold uppercase tracking-wider bg-primary text-primary-foreground px-1.5 py-0.5 rounded-md">
                                Maker
                              </span>
                            )}
                          </div>
                          <div className="flex items-center gap-3">
                            <span className="text-xs text-muted-foreground">
                              {new Date(comment.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}
                            </span>
                            {!comment.isDeleted && session?.user?.id === comment.userId && (
                              <div className="flex gap-2">
                                <button onClick={() => { setEditCommentId(comment.id); setEditContent(comment.content); }} className="text-xs text-muted-foreground hover:text-primary">Düzenle</button>
                                <button onClick={() => handleCommentDelete(comment.id)} className="text-xs text-muted-foreground hover:text-red-500">Sil</button>
                              </div>
                            )}
                          </div>
                        </div>
                        
                        {comment.isDeleted ? (
                          <p className="text-muted-foreground italic text-sm pt-1">Bu yorum silinmiştir.</p>
                        ) : editCommentId === comment.id ? (
                          <div className="mt-2 space-y-2">
                            <Textarea value={editContent} onChange={(e) => setEditContent(e.target.value)} className="min-h-[60px]" />
                            <div className="flex gap-2 justify-end">
                              <Button size="sm" variant="ghost" onClick={() => setEditCommentId(null)}>İptal</Button>
                              <Button size="sm" onClick={() => handleCommentEdit(comment.id)}>Kaydet</Button>
                            </div>
                          </div>
                        ) : (
                          <>
                            <p className="text-foreground leading-relaxed text-sm pt-1">
                              {comment.content}
                              {comment.updatedAt && <span className="text-[10px] text-muted-foreground ml-2">(Düzenlendi)</span>}
                            </p>
                            <div className="pt-2">
                              <button 
                                onClick={() => {
                                  setReplyToId(replyToId === comment.id ? null : comment.id);
                                  setReplyContent("");
                                }}
                                className="text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
                              >
                                {replyToId === comment.id ? "İptal Et" : "Cevapla"}
                              </button>
                            </div>
                          </>
                        )}
                        
                        {/* Reply Form */}
                        {replyToId === comment.id && !comment.isDeleted && (
                          <div className="mt-3 flex gap-3 flex-col sm:flex-row items-end sm:items-center">
                            <Textarea 
                              placeholder="Cevabını yaz..."
                              className="min-h-[40px] h-[40px] resize-none bg-background py-2 text-sm"
                              value={replyContent}
                              onChange={(e) => setReplyContent(e.target.value)}
                            />
                            <Button size="sm" onClick={() => handleCommentSubmit(comment.id)} disabled={!replyContent.trim() || isSubmittingComment}>
                              Gönder
                            </Button>
                          </div>
                        )}
                      </div>
                    </div>
                    
                    {/* Nested Replies */}
                    {comment.replies?.length > 0 && (
                      <div className="pl-6 md:pl-12 space-y-4 border-l-2 ml-4 md:ml-6 mt-4">
                        {comment.replies.map((reply) => (
                          <div key={reply.id} className={`flex gap-4 p-4 rounded-xl transition-colors ${reply.userId === product.makerId ? 'bg-primary/5 border border-primary/20' : 'bg-muted/30'} ${reply.isDeleted ? 'opacity-60' : ''}`}>
                            <div className={`h-8 w-8 rounded-full flex-shrink-0 flex items-center justify-center font-bold text-xs text-white ${reply.userId === product.makerId ? 'bg-primary' : 'bg-secondary text-secondary-foreground'}`}>
                              {reply.isDeleted ? "-" : (reply.userName?.[0]?.toUpperCase() || "U")}
                            </div>
                            <div className="flex-1 space-y-1">
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  <span className="font-medium text-sm">{reply.isDeleted ? "[Silinmiş Kullanıcı]" : reply.userName}</span>
                                  {!reply.isDeleted && reply.userId === product.makerId && (
                                    <span className="text-[10px] font-bold uppercase tracking-wider bg-primary text-primary-foreground px-1.5 py-0.5 rounded-md">
                                      Maker
                                    </span>
                                  )}
                                </div>
                                <div className="flex items-center gap-3">
                                  <span className="text-[10px] text-muted-foreground">
                                    {new Date(reply.createdAt).toLocaleDateString('tr-TR')}
                                  </span>
                                  {!reply.isDeleted && session?.user?.id === reply.userId && (
                                    <div className="flex gap-2">
                                      <button onClick={() => { setEditCommentId(reply.id); setEditContent(reply.content); }} className="text-[10px] text-muted-foreground hover:text-primary">Düzenle</button>
                                      <button onClick={() => handleCommentDelete(reply.id)} className="text-[10px] text-muted-foreground hover:text-red-500">Sil</button>
                                    </div>
                                  )}
                                </div>
                              </div>
                              
                              {reply.isDeleted ? (
                                <p className="text-muted-foreground italic text-sm pt-1">Bu cevap silinmiştir.</p>
                              ) : editCommentId === reply.id ? (
                                <div className="mt-2 space-y-2">
                                  <Textarea value={editContent} onChange={(e) => setEditContent(e.target.value)} className="min-h-[50px] text-sm" />
                                  <div className="flex gap-2 justify-end">
                                    <Button size="sm" variant="ghost" onClick={() => setEditCommentId(null)}>İptal</Button>
                                    <Button size="sm" onClick={() => handleCommentEdit(reply.id)}>Kaydet</Button>
                                  </div>
                                </div>
                              ) : (
                                <p className="text-muted-foreground leading-relaxed text-sm">
                                  {reply.content}
                                  {reply.updatedAt && <span className="text-[10px] text-muted-foreground ml-2">(Düzenlendi)</span>}
                                </p>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))
              )}
            </div>

          </div>
        </div>

        {/* Right Column (Sidebar) */}
        <div className="space-y-6">
          <div className="rounded-2xl border bg-card p-6 shadow-sm">
            <h4 className="font-bold mb-4 uppercase tracking-wider text-xs text-muted-foreground">Yapımcı (Maker)</h4>
            <div className="flex items-center gap-3">
              {maker?.avatarUrl ? (
                <div className="h-12 w-12 rounded-full overflow-hidden border shadow-inner">
                  <Image src={maker.avatarUrl} alt={maker.fullName || "Maker"} width={48} height={48} className="object-cover w-full h-full" />
                </div>
              ) : (
                <div className="h-12 w-12 rounded-full bg-gradient-to-tr from-purple-500 to-blue-500 flex items-center justify-center text-white font-bold text-lg shadow-inner">
                  {maker?.fullName?.charAt(0) || "M"}
                </div>
              )}
              <div>
                <Link href={maker?.username ? `/profile/${maker.username}` : "#"} className="font-semibold hover:underline">
                  {maker?.fullName || "Bilinmeyen Maker"}
                </Link>
                <div className="text-xs text-muted-foreground">@{maker?.username || product.makerId.substring(0,8)}</div>
              </div>
            </div>
            <Button 
              variant={maker?.isFollowing ? "secondary" : "outline"} 
              className="w-full mt-4 rounded-xl"
              onClick={handleFollowMaker}
              disabled={!maker || session?.user?.id === product.makerId}
            >
              {maker?.isFollowing ? "Takipten Çık" : "Takip Et"} {maker?.followerCount !== undefined && `(${maker.followerCount})`}
            </Button>
          </div>

          <div className="rounded-2xl border bg-card p-6 shadow-sm">
            <h4 className="font-bold mb-4 uppercase tracking-wider text-xs text-muted-foreground">Aksiyonlar</h4>
            <div className="space-y-2">
              <Button 
                variant="secondary" 
                className="w-full justify-start gap-2 rounded-xl"
                onClick={() => {
                  if (!session) setIsLoginModalOpen(true);
                  else setIsCollectionModalOpen(true);
                }}
              >
                <Bookmark className="h-4 w-4" /> Koleksiyona Ekle
              </Button>
              <Button variant="secondary" className="w-full justify-start gap-2 rounded-xl">
                <Share2 className="h-4 w-4" /> Paylaş
              </Button>
              <Button variant="ghost" className="w-full justify-start gap-2 rounded-xl text-muted-foreground hover:text-red-500 hover:bg-red-50">
                <AlertCircle className="h-4 w-4" /> Şikayet Et
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Recommendations Section */}
      {recommendations && recommendations.length > 0 && (
        <div className="pt-10 border-t border-border/50">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-10 h-10 rounded-xl bg-purple-500/10 text-purple-600 flex items-center justify-center">
              <Star className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-2xl font-bold">Benzer Ürünler</h3>
              <p className="text-sm text-muted-foreground">Yapay zeka analizine göre bu ürünü sevenler bunları da inceledi.</p>
            </div>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {recommendations.map((rec) => (
              <Link href={`/product/${rec.slug}`} key={rec.id} className="group flex items-start gap-4 p-4 rounded-2xl border bg-card hover:border-purple-500/30 hover:shadow-md transition-all">
                <div className="relative h-16 w-16 rounded-xl bg-muted overflow-hidden flex-shrink-0 flex items-center justify-center border shadow-sm">
                  {rec.thumbnailUrl ? (
                    <Image src={rec.thumbnailUrl} alt={rec.name} fill sizes="64px" className="object-cover group-hover:scale-105 transition-transform" />
                  ) : (
                    <span className="font-bold text-muted-foreground">{rec.name.substring(0, 2).toUpperCase()}</span>
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <h4 className="font-bold text-base truncate group-hover:text-purple-600 transition-colors">{rec.name}</h4>
                  <p className="text-sm text-muted-foreground line-clamp-2 mt-1">{rec.tagline}</p>
                  <div className="flex items-center gap-1.5 mt-2 text-xs font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 w-fit px-2 py-0.5 rounded-full">
                    <ArrowUp className="w-3 h-3" /> {rec.upvotes}
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
      <LoginModal isOpen={isLoginModalOpen} onClose={() => setIsLoginModalOpen(false)} />
      {product && (
        <AddToCollectionModal 
          isOpen={isCollectionModalOpen} 
          onClose={() => setIsCollectionModalOpen(false)} 
          productId={product.id} 
        />
      )}

      {/* Image Lightbox */}
      {selectedImage && (
        <div 
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 backdrop-blur-sm p-4"
          onClick={() => setSelectedImage(null)}
        >
          <button 
            className="absolute top-6 right-6 text-white/70 hover:text-white bg-black/50 hover:bg-black/80 rounded-full p-2 transition-colors"
            onClick={() => setSelectedImage(null)}
          >
            <X className="w-6 h-6" />
          </button>
          <Image
            src={selectedImage}
            alt="Enlarged"
            width={1600}
            height={900}
            className="max-w-full max-h-[90vh] object-contain rounded-xl shadow-2xl animate-in zoom-in-95 duration-200" 
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}
    </div>
  );
}
