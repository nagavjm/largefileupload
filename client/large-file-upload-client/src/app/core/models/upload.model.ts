export type UploadStatus =
  | 'Pending'
  | 'Uploading'
  | 'Processing'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'Expired';

export type UploadStrategy = 'DirectSas' | 'ManualBlockBlob';

export interface UploadSessionDto {
  id: string;
  userId: string;
  originalFileName: string;
  blobName: string;
  containerName: string;
  fileSize: number;
  contentType: string;
  uploadStrategy: UploadStrategy;
  chunkSize?: number;
  totalChunks?: number;
  uploadedChunks: number;
  status: UploadStatus;
  progressPercentage: number;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface DirectUploadInitiationResponse {
  uploadId: string;
  blobName: string;
  containerName: string;
  sasUrl: string;
  expiresAt: string;
  uploadStrategy: string;
  maxBlockSizeBytes: number;
}

export interface CompleteDirectUploadResponse {
  uploadId: string;
  status: string;
  blobUri: string;
  verifiedBytes: number;
  completedAt: string;
}

export interface ChunkedUploadInitiationResponse {
  uploadId: string;
  blobName: string;
  containerName: string;
  sasUrl: string;
  chunkSize: number;
  totalChunks: number;
  blockIds: string[];
  expiresAt: string;
}

export interface ChunkedUploadBlocksResponse {
  uploadId: string;
  totalChunks: number;
  stagedBlockCount: number;
  stagedBlockIds: string[];
  missingBlockIndexes: number[];
  isResumable: boolean;
}

export interface ChunkedUploadCommitResponse {
  uploadId: string;
  status: string;
  blobUri: string;
  totalBlocksCommitted: number;
  verifiedBytes: number;
  completedAt: string;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UploadProgressState {
  uploadId?: string;
  fileName: string;
  fileSize: number;
  uploadedBytes: number;
  percentage: number;
  status: UploadStatus | 'Idle';
  speedBytesPerSec: number;
  timeRemainingSec?: number;
  errorMessage?: string;
}
