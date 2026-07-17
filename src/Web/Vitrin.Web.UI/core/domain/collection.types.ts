import type { ProductApiModel } from './product.types';

export enum CollectionVisibility {
  Private = 0,
  Public = 1,
  Shared = 2,
}

export enum CollectionCollaboratorRole {
  Viewer = 0,
  Editor = 1,
}

export interface CollectionCollaborator {
  userId: string;
  role: CollectionCollaboratorRole;
  addedAt: string;
}

export interface CollectionSummary {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  userId: string;
  visibility: CollectionVisibility;
  createdAt: string;
  productCount: number;
  collaboratorCount: number;
  isOwner: boolean;
  canEdit: boolean;
}

export interface CollectionDetail {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  userId: string;
  visibility: CollectionVisibility;
  createdAt: string;
  products?: ProductApiModel[];
  collaborators: CollectionCollaborator[];
  isOwner: boolean;
  canEdit: boolean;
}
