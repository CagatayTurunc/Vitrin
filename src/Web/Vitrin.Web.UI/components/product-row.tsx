'use client'

import Image from 'next/image'
import { useState } from 'react'
import { ChevronUp } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { Product } from '@/core/domain/product.types'
import { useProductStore } from '@/core/application/useProductStore'

export function ProductRow({ product }: { product: Product }) {
  const { upvote } = useProductStore()

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
        <div className="mt-2 flex flex-wrap items-center gap-1.5">
          {product.tags.map((tag) => (
            <Badge
              key={tag}
              variant="secondary"
              className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground"
            >
              {tag}
            </Badge>
          ))}
        </div>
      </div>

      {/* Upvote */}
      <button
        type="button"
        onClick={() => upvote(product.id)}
        aria-label={`${product.name} için oy ver`}
        className={cn(
          'relative z-10 flex h-14 w-14 shrink-0 flex-col items-center justify-center rounded-xl border text-center transition-all sm:h-16 sm:w-16',
          'border-border bg-background text-foreground hover:border-primary hover:text-primary active:scale-95'
        )}
      >
        <ChevronUp
          className={cn(
            'h-5 w-5 transition-transform',
            'group-hover:-translate-y-px'
          )}
          strokeWidth={2.5}
          aria-hidden="true"
        />
        <span className="text-sm font-bold tabular-nums leading-none">
          {product.votes}
        </span>
      </button>
    </div>
  )
}
