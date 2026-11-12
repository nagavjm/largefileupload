import { UploadStatus } from '../../../core/models/upload.model';

export type ChunkStatus = 'queued' | 'uploading' | 'staged' | 'failed';

export interface ChunkInfo {
  index: number;
  blockId: string;
  startByte: number;
  endByte: number;
  size: number;
  status: ChunkStatus;
  retryCount: number;
  errorMessage?: string;
}

export interface ChunkedUploadState {
  uploadId?: string;
  fileName: string;
  fileSize: number;
  chunkSize: number;
  totalChunks: number;
  stagedChunks: number;
  status: UploadStatus | 'Idle' | 'Paused';
  uploadedBytes: number;
  percentage: number;
  speedBytesPerSec: number;
  chunks: ChunkInfo[];
  concurrency: number;
  isResumed: boolean;
  errorMessage?: string;
}
