export interface UserSummary {
  id: string;
  username: string;
  fullName?: string | null;
  avatarUrl?: string | null;
}

export interface LeaderboardStreakUser extends UserSummary {
  currentStreak: number;
}

export interface LeaderboardMaker extends UserSummary {
  followerCount: number;
}

export interface LeaderboardData {
  topStreaks: LeaderboardStreakUser[];
  topMakers: LeaderboardMaker[];
}

export interface UserBadge {
  name: string;
  icon: string;
}

export interface UserProfile extends UserSummary {
  email?: string | null;
  headline?: string | null;
  about?: string | null;
  websiteUrl?: string | null;
  githubUrl?: string | null;
  linkedInUrl?: string | null;
  createdAt: string;
  role: number | string;
  followerCount: number;
  followingCount: number;
  isFollowing?: boolean;
  currentStreak?: number;
  badges?: UserBadge[];
}
