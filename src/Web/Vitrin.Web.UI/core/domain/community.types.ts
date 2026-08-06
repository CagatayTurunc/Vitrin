export enum CommunityThreadCategory { General, Maker, Technical, Feedback, Collaboration, Support, Changelog }
export enum CommunityThreadKind { Discussion, Question, Feedback, Poll, Ama, BuildInPublic }

export interface CommunityThread {
  id: string; productId?: string | null; authorId: string; title: string; slug: string; body: string;
  category: CommunityThreadCategory; kind: CommunityThreadKind; isPinned: boolean; isLocked: boolean;
  viewCount: number; replyCount: number; reactionCount: number; followerCount: number;
  isReacted: boolean; isFollowing: boolean; createdAtUtc: string; updatedAtUtc: string;
}
export interface CommunityReply { id: string; authorId: string; parentReplyId?: string | null; body: string; isOfficial: boolean; reactionCount: number; isReacted: boolean; createdAtUtc: string; }
export interface CommunityThreadDetail extends CommunityThread { replies: CommunityReply[] }

export const communityCategoryLabels: Record<CommunityThreadCategory, string> = {
  [CommunityThreadCategory.General]: "Genel", [CommunityThreadCategory.Maker]: "Maker",
  [CommunityThreadCategory.Technical]: "Teknik", [CommunityThreadCategory.Feedback]: "Geri bildirim",
  [CommunityThreadCategory.Collaboration]: "İş birliği", [CommunityThreadCategory.Support]: "Destek",
  [CommunityThreadCategory.Changelog]: "Güncellemeler",
};
