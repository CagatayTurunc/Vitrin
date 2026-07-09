'use client'

import Image from 'next/image'
import { useState } from 'react'
import { ChevronUp } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { Product } from '@/core/domain/product.types'
import { useProductStore } from '@/core/application/useProductStore'
import { useSession } from 'next-auth/react'
import { LoginModal } from '@/components/login-modal'

export function ProductRow({ product }: { product: Product }) {
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
          <a href="#" className="outline-none after:absolute after:inset-0">
            {product.name}
          </a>
        </h3>
        <p className="mt-0.5 truncate text-sm text-muted-foreground">
          {product.description}
        </p>
        {/* Tags / Topics */}
        <div className="mt-2 flex items-center gap-2">
          {product.topics && product.topics.length > 0 ? (
            product.topics.map(t => (
              <span key={t.id} className="inline-flex items-center rounded-md bg-secondary px-2 py-1 text-xs font-medium text-secondary-foreground">
                {t.name}
              </span>
            ))
          ) : (
            <span className="inline-flex items-center rounded-md bg-secondary/50 px-2 py-1 text-xs font-medium text-muted-foreground">
              Etiket Yok
            </span>
          )}
        </div>
      </div>

      {/* Upvote */}
      <button
        type="button"
        onClick={handleUpvote}
        aria-label={`${product.name} için oy ver`}
        className={cn(
          'relative z-10 flex h-14 w-14 shrink-0 flex-col items-center justify-center rounded-xl border text-center transition-all sm:h-16 sm:w-16',
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

      <LoginModal 
        isOpen={isLoginModalOpen} 
        onClose={() => setIsLoginModalOpen(false)} 
      />
    </div>
  )
}
