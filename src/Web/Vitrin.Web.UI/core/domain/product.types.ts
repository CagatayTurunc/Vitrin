export interface Product {
  id: string;
  rank?: number;
  name: string;
  slug: string;
  description: string;
  publishedAt: string;
  image: string;
  topics?: { id: string; name: string; slug: string }[];
  votes: number;
  views?: number;
  comments?: number;
  trendScore?: number;
  searchScore?: number;
  matchType?: string | null;
  makerTier?: 'Free' | 'ProMaker' | 'Enterprise' | null;
}

export interface Topic {
  id: string;
  name: string;
  slug: string;
}

export interface ProductCategory {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentId?: string | null;
  sortOrder?: number;
  productCount?: number;
}

export interface ProductLaunchSummary {
  id: string;
  sequenceNumber: number;
  versionLabel: string;
  status: number;
  scheduledAtUtc?: string | null;
  publishedAtUtc?: string | null;
  isFeatured: boolean;
  finalRank?: number | null;
  finalScore?: number | null;
}

export interface ProductApiModel {
  id: string;
  name: string;
  slug: string;
  tagline?: string | null;
  description: string;
  publishedAt: string;
  thumbnailUrl?: string | null;
  topics?: Topic[];
  upvotes?: number;
  viewCount?: number;
  commentCount?: number;
  trendScore?: number;
  searchScore?: number;
  matchType?: string | null;
  categories?: ProductCategory[];
  activeLaunch?: ProductLaunchSummary | null;
  pricingModel?: string | null;
  platforms?: string[];
  hasTrSupport?: boolean;
  hasKvkk?: boolean;
  reviewScore?: number;
  verifiedUsersCount?: number;
  makerTierSnapshot?: 'Free' | 'ProMaker' | 'Enterprise' | null;
}

export interface ProductDetailApiModel extends ProductApiModel {
  makerId: string;
  tagline: string;
  thumbnailUrl?: string | null;
  galleryUrls?: string[];
  websiteUrl?: string | null;
  upvotes: number;
}

export interface RankedLaunch {
  launchId: string;
  productId: string;
  productName: string;
  productSlug: string;
  sequenceNumber: number;
  versionLabel: string;
  tagline: string;
  thumbnailUrl: string;
  publishedAtUtc: string;
  isFeatured: boolean;
  rank: number;
  score: number;
  engagementScore: number;
  discoveryScore: number;
  freshnessScore: number;
  upvotes: number;
  comments: number;
  views: number;
  categories: ProductCategory[];
}

export interface DailyLaunches {
  date: string;
  startsAtUtc: string;
  endsAtUtc: string;
  isToday: boolean;
  items: RankedLaunch[];
}

export interface LaunchDaySummary {
  date: string;
  launchCount: number;
  winner?: RankedLaunch | null;
}

export interface LaunchArchive {
  from: string;
  to: string;
  days: LaunchDaySummary[];
}

export interface UpcomingLaunch {
  launchId: string;
  productId: string;
  productName: string;
  productSlug: string;
  versionLabel: string;
  tagline: string;
  thumbnailUrl: string;
  scheduledAtUtc: string;
  categories: ProductCategory[];
}

export interface UpcomingLaunches {
  fromUtc: string;
  toUtc: string;
  items: UpcomingLaunch[];
}

export interface CursorPage<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

export type ProductSort = 'relevance' | 'newest' | 'trending' | 'most_voted' | 'most_commented' | 'most_viewed';

export interface ProductFilters {
  q?: string;
  topics?: string[];
  minUpvotes?: number;
  minComments?: number;
  minViews?: number;
  publishedFrom?: string;
  publishedTo?: string;
  sort?: ProductSort;
  city?: string;
  university?: string;
  technopark?: string;
}

export interface SavedSearch {
  id: string;
  name: string;
  query?: string | null;
  topics: string[];
  minUpvotes?: number | null;
  minComments?: number | null;
  minViews?: number | null;
  publishedFrom?: string | null;
  publishedTo?: string | null;
  sort: ProductSort;
  notifyOnNewMatches: boolean;
  createdAtUtc: string;
  city?: string | null;
  university?: string | null;
  technopark?: string | null;
}
