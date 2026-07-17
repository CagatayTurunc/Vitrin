'use client'

import Image from 'next/image'
import Link from 'next/link'
import { useState } from 'react'
import { ChevronUp, Eye, Flame, GitCompareArrows, MessageCircle, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Product } from '@/core/domain/product.types'
import { useProductStore } from '@/core/application/useProductStore'
import { useSession } from 'next-auth/react'
import { LoginModal } from '@/components/login-modal'

export function ProductRow({ product, onRemove }: { product: Product; onRemove?: (productId: string) => void }) {
  const { upvote, votedProductIds } = useProductStore()
  const { data: session } = useSession()
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false)

  const handleUpvote = (e: React.MouseEvent) => {
    e.preventDefault();
    if (!session?.accessToken) {
      setIsLoginModalOpen(true);
      return;
    }
    upvote(product.id, session.accessToken as string);
  }

  const hasVoted = votedProductIds.includes(product.id);

  return (
    <div className="group relative flex items-center gap-3 rounded-2xl border border-transparent px-3 py-4 transition-colors hover:border-border hover:bg-muted/50 sm:gap-4 sm:px-4">
      {/* Rank + Thumbnail */}
      <div className="flex shrink-0 items-center gap-3">
        <span className="hidden w-5 text-right text-sm font-semibold tabular-nums text-muted-foreground sm:block">
          {product.rank}
        </span>
        <Image
          src={product.image || '/placeholder.svg'}
          alt={`${product.name} logosu`}
          width={64}
          height={64}
          className="h-14 w-14 rounded-xl border border-border object-cover sm:h-16 sm:w-16"
        />
      </div>

      {/* Middle */}
      <div className="min-w-0 flex-1">
        <h3 className="truncate text-base font-bold text-foreground sm:text-lg">
          <Link href={`/product/${product.slug}`} className="outline-none after:absolute after:inset-0">
            {product.name}
          </Link>
        </h3>
        <p className="mt-0.5 truncate text-sm text-muted-foreground">
          {product.description}
        </p>
        {/* Tags / Topics */}
        <div className="mt-2 flex items-center gap-2">
          {product.topics && product.topics.length > 0 ? (
            product.topics.map(t => (
              <Link key={t.id} href={`/topic/${t.slug}`} className="inline-flex items-center rounded-md bg-secondary px-2 py-1 text-xs font-medium text-secondary-foreground hover:bg-secondary/80 transition-colors z-20">
                {t.name}
              </Link>
            ))
          ) : (
            <span className="inline-flex items-center rounded-md bg-secondary/50 px-2 py-1 text-xs font-medium text-muted-foreground">
              Etiket Yok
            </span>
          )}
          <span className="ml-auto hidden items-center gap-3 text-xs text-muted-foreground md:flex">
            <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" /> {product.views ?? 0}</span>
            <span className="inline-flex items-center gap-1"><MessageCircle className="h-3.5 w-3.5" /> {product.comments ?? 0}</span>
            {(product.trendScore ?? 0) > 0 && (
              <span className="inline-flex items-center gap-1 rounded-full bg-orange-500/10 px-2 py-1 font-semibold text-orange-600 dark:text-orange-400">
                <Flame className="h-3.5 w-3.5" /> {product.trendScore?.toFixed(1)}
              </span>
            )}
            {product.matchType && (
              <span className="rounded-full bg-primary/10 px-2 py-1 font-semibold text-primary">
                {product.matchType === 'typo' ? 'Benzer eşleşme' : product.matchType === 'full_text' ? 'İçerik eşleşmesi' : product.matchType === 'topic' ? 'Kategori eşleşmesi' : 'Güçlü eşleşme'}
              </span>
            )}
          </span>
        </div>
      </div>

      <div className="relative z-10 flex shrink-0 items-center gap-2">
        <div className="hidden flex-col gap-1 sm:flex">
          <Link href={`/compare?ids=${product.id}`} aria-label={`${product.name} ürününü karşılaştır`} title="Karşılaştır" className="flex h-7 w-7 items-center justify-center rounded-lg border border-border bg-background text-muted-foreground transition-colors hover:border-primary hover:text-primary">
            <GitCompareArrows className="h-3.5 w-3.5" />
          </Link>
          {onRemove && (
            <button type="button" onClick={() => onRemove(product.id)} aria-label={`${product.name} ürününü koleksiyondan çıkar`} title="Koleksiyondan çıkar" className="flex h-7 w-7 items-center justify-center rounded-lg border border-border bg-background text-muted-foreground transition-colors hover:border-destructive hover:text-destructive">
              <Trash2 className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        <button
          type="button"
          onClick={handleUpvote}
          aria-label={`${product.name} için oy ver`}
          className={cn(
            'flex h-14 w-14 shrink-0 flex-col items-center justify-center rounded-xl border text-center transition-all sm:h-16 sm:w-16',
            hasVoted
              ? 'border-primary bg-primary/10 text-primary hover:bg-primary/20'
              : 'border-border bg-background text-foreground hover:border-primary hover:text-primary active:scale-95'
          )}
        >
          <ChevronUp
            className={cn(
              'h-5 w-5 transition-transform',
              !hasVoted && 'group-hover:-translate-y-px',
              hasVoted && 'text-primary'
            )}
            strokeWidth={2.5}
            aria-hidden="true"
          />
          <span className="text-sm font-bold tabular-nums leading-none">
            {product.votes}
          </span>
        </button>
      </div>

      <LoginModal 
        isOpen={isLoginModalOpen} 
        onClose={() => setIsLoginModalOpen(false)} 
      />
    </div>
  )
}
