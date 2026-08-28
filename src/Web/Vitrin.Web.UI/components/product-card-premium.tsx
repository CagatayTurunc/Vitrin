"use client";

import Image from "next/image";
import Link from "next/link";
import { cn } from "@/lib/utils";

type SubscriptionTier = "Free" | "ProMaker" | "Enterprise";

interface Product {
  id: string;
  name: string;
  slug: string;
  tagline: string;
  thumbnailUrl: string;
  upvoteCount: number;
  commentCount: number;
  maker: {
    name: string;
    avatarUrl: string;
    tier: SubscriptionTier;
  };
}

interface ProductCardProps {
  product: Product;
  index?: number;
}

export function ProductCard({ product, index = 0 }: ProductCardProps) {
  const tierConfig = getTierConfig(product.maker.tier);

  const handleClick = () => {
    // Track premium product clicks
    if (product.maker.tier !== "Free" && typeof window !== "undefined") {
      // Analytics tracking
      if ((window as any).gtag) {
        (window as any).gtag("event", "premium_product_clicked", {
          product_id: product.id,
          maker_tier: product.maker.tier,
          position: index,
        });
      }
    }
  };

  return (
    <Link
      href={`/products/${product.slug}`}
      onClick={handleClick}
      className={cn(
        "group relative block rounded-xl overflow-hidden transition-all duration-300",
        "focus:outline-none focus:ring-2 focus:ring-offset-2",
        tierConfig.cardClass,
        tierConfig.focusRingClass
      )}
    >
      {/* Glow effect background (premium only) */}
      {tierConfig.hasGlow && (
        <div
          className={cn(
            "absolute inset-0 rounded-xl opacity-0 group-hover:opacity-100 transition-opacity duration-500 -z-10",
            tierConfig.glowClass
          )}
        />
      )}

      {/* Premium Badge */}
      {tierConfig.badge && (
        <div className="absolute top-3 left-3 z-20">
          <div className={cn("flex items-center gap-1.5 px-3 py-1.5 rounded-full shadow-lg backdrop-blur-sm", tierConfig.badgeClass)}>
            <span className="text-xs font-bold">{tierConfig.badge}</span>
          </div>
        </div>
      )}

      {/* Thumbnail */}
      <div className="relative aspect-video overflow-hidden bg-gradient-to-br from-gray-100 to-gray-200">
        <Image
          src={product.thumbnailUrl || "/placeholder-product.png"}
          alt={product.name}
          fill
          className="object-cover transition-transform duration-500 group-hover:scale-110"
          sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
        />
        
        {/* Overlay gradient (premium only) */}
        {tierConfig.hasOverlay && (
          <div className={cn(
            "absolute inset-0 opacity-0 group-hover:opacity-30 transition-opacity duration-300",
            tierConfig.overlayClass
          )} />
        )}
      </div>

      {/* Content */}
      <div className="p-4 bg-white">
        {/* Product Name */}
        <h3 className={cn(
          "font-semibold text-lg line-clamp-1 transition-colors",
          tierConfig.titleClass
        )}>
          {product.name}
        </h3>

        {/* Tagline */}
        <p className="text-sm text-gray-600 line-clamp-2 mt-1 mb-3">
          {product.tagline}
        </p>

        {/* Maker Info */}
        <div className="flex items-center gap-2 mb-3">
          <Image
            src={product.maker.avatarUrl || "/default-avatar.png"}
            alt={product.maker.name}
            width={24}
            height={24}
            className="rounded-full"
          />
          <span className="text-sm text-gray-700 font-medium">
            {product.maker.name}
          </span>
        </div>

        {/* Stats */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 text-sm text-gray-500">
            <span className="flex items-center gap-1">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path d="M2 10.5a1.5 1.5 0 113 0v6a1.5 1.5 0 01-3 0v-6zM6 10.333v5.43a2 2 0 001.106 1.79l.05.025A4 4 0 008.943 18h5.416a2 2 0 001.962-1.608l1.2-6A2 2 0 0015.56 8H12V4a2 2 0 00-2-2 1 1 0 00-1 1v.667a4 4 0 01-.8 2.4L6.8 7.933a4 4 0 00-.8 2.4z" />
              </svg>
              {product.upvoteCount}
            </span>
            <span className="flex items-center gap-1">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clipRule="evenodd" />
              </svg>
              {product.commentCount}
            </span>
          </div>

          {/* Premium indicator */}
          {tierConfig.premiumIndicator && (
            <div className="flex items-center text-xs text-gray-400">
              {tierConfig.premiumIndicator}
            </div>
          )}
        </div>
      </div>

      {/* Border decoration (premium only) */}
      {tierConfig.hasBorder && (
        <div className={cn(
          "absolute inset-0 rounded-xl pointer-events-none",
          tierConfig.borderClass
        )} />
      )}
    </Link>
  );
}

function getTierConfig(tier: SubscriptionTier) {
  switch (tier) {
    case "ProMaker":
      return {
        badge: "🏆 PRO",
        badgeClass: "bg-gradient-to-r from-blue-500 to-purple-600 text-white",
        cardClass: "border-2 border-blue-200 hover:border-blue-400 bg-gradient-to-br from-blue-50/30 to-purple-50/30 hover:shadow-2xl hover:-translate-y-2",
        focusRingClass: "focus:ring-blue-500",
        titleClass: "group-hover:text-blue-600",
        hasBorder: true,
        borderClass: "bg-gradient-to-br from-blue-500/20 to-purple-600/20",
        hasGlow: true,
        glowClass: "bg-gradient-to-br from-blue-500/30 to-purple-600/30 blur-xl",
        hasOverlay: true,
        overlayClass: "bg-gradient-to-t from-blue-600/50 to-transparent",
        premiumIndicator: "⚡"
      };

    case "Enterprise":
      return {
        badge: "💎 FEATURED",
        badgeClass: "bg-gradient-to-r from-yellow-400 via-pink-500 to-purple-600 text-white animate-pulse",
        cardClass: "border-4 border-transparent bg-gradient-to-br from-yellow-50/50 via-pink-50/50 to-purple-50/50 hover:shadow-3xl hover:-translate-y-3 ring-4 ring-yellow-400/20",
        focusRingClass: "focus:ring-pink-500",
        titleClass: "group-hover:text-pink-600 font-bold",
        hasBorder: true,
        borderClass: "bg-gradient-to-br from-yellow-400/30 via-pink-500/30 to-purple-600/30",
        hasGlow: true,
        glowClass: "bg-gradient-to-br from-yellow-400/40 via-pink-500/40 to-purple-600/40 blur-2xl",
        hasOverlay: true,
        overlayClass: "bg-gradient-to-t from-pink-600/60 to-transparent",
        premiumIndicator: "🔥"
      };

    default: // Free
      return {
        badge: null,
        badgeClass: "",
        cardClass: "border border-gray-200 hover:border-gray-300 hover:shadow-lg hover:-translate-y-1 bg-white",
        focusRingClass: "focus:ring-gray-400",
        titleClass: "group-hover:text-gray-900",
        hasBorder: false,
        borderClass: "",
        hasGlow: false,
        glowClass: "",
        hasOverlay: false,
        overlayClass: "",
        premiumIndicator: null
      };
  }
}

// Featured Product Card (Larger, Enterprise only)
export function FeaturedProductCard({ product }: ProductCardProps) {
  if (product.maker.tier !== "Enterprise") {
    return <ProductCard product={product} />;
  }

  return (
    <Link
      href={`/products/${product.slug}`}
      className="group relative block rounded-2xl overflow-hidden border-4 border-transparent bg-gradient-to-br from-yellow-50 via-pink-50 to-purple-50 hover:shadow-3xl hover:-translate-y-2 transition-all duration-500"
    >
      {/* Animated glow */}
      <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-yellow-400/40 via-pink-500/40 to-purple-600/40 blur-3xl opacity-0 group-hover:opacity-100 transition-opacity duration-500 -z-10 animate-pulse" />

      {/* Featured badge - larger */}
      <div className="absolute top-4 left-4 z-20">
        <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-gradient-to-r from-yellow-400 via-pink-500 to-purple-600 text-white shadow-2xl backdrop-blur-sm animate-pulse">
          <span className="text-sm font-bold">💎 FEATURED</span>
        </div>
      </div>

      {/* Large thumbnail */}
      <div className="relative aspect-[16/9] overflow-hidden bg-gradient-to-br from-yellow-100 to-pink-100">
        <Image
          src={product.thumbnailUrl || "/placeholder-product.png"}
          alt={product.name}
          fill
          className="object-cover transition-transform duration-700 group-hover:scale-110"
          priority
        />
        <div className="absolute inset-0 bg-gradient-to-t from-pink-600/40 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
      </div>

      {/* Content */}
      <div className="p-6 bg-white">
        <h2 className="text-2xl font-bold line-clamp-2 group-hover:text-pink-600 transition-colors">
          {product.name}
        </h2>
        <p className="text-base text-gray-600 line-clamp-3 mt-2 mb-4">
          {product.tagline}
        </p>

        {/* Maker */}
        <div className="flex items-center gap-3 mb-4">
          <Image
            src={product.maker.avatarUrl || "/default-avatar.png"}
            alt={product.maker.name}
            width={32}
            height={32}
            className="rounded-full ring-2 ring-pink-200"
          />
          <div>
            <p className="font-semibold text-gray-900">{product.maker.name}</p>
            <p className="text-sm text-pink-600">Enterprise Maker</p>
          </div>
        </div>

        {/* Stats */}
        <div className="flex items-center gap-6 text-gray-600">
          <span className="flex items-center gap-2">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M2 10.5a1.5 1.5 0 113 0v6a1.5 1.5 0 01-3 0v-6zM6 10.333v5.43a2 2 0 001.106 1.79l.05.025A4 4 0 008.943 18h5.416a2 2 0 001.962-1.608l1.2-6A2 2 0 0015.56 8H12V4a2 2 0 00-2-2 1 1 0 00-1 1v.667a4 4 0 01-.8 2.4L6.8 7.933a4 4 0 00-.8 2.4z" />
            </svg>
            <span className="font-semibold">{product.upvoteCount}</span>
          </span>
          <span className="flex items-center gap-2">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clipRule="evenodd" />
            </svg>
            <span className="font-semibold">{product.commentCount}</span>
          </span>
          <span className="ml-auto text-pink-600 font-semibold">🔥 Trending</span>
        </div>
      </div>

      {/* Border decoration */}
      <div className="absolute inset-0 rounded-2xl bg-gradient-to-br from-yellow-400/30 via-pink-500/30 to-purple-600/30 pointer-events-none" />
    </Link>
  );
}
