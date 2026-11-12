import { Injectable, inject } from '@angular/core';
import { BlockBlobClient } from '@azure/storage-blob';
import { ApiService } from '../../../core/services/api.service';
import { ChunkedStorageService } from './chunked-storage.service';
import { ChunkInfo, ChunkedUploadState } from '../models/chunk.model';
import {
  ChunkedUploadCommitResponse,
  ChunkedUploadInitiationResponse
} from '../../../core/models/upload.model';

@Injectable({
  providedIn: 'root'
})
export class BlockOrchestratorService {
  private readonly apiService = inject(ApiService);
  private readonly storageService = inject(ChunkedStorageService);

  private abortController: AbortController | null = null;
  private isPaused = false;
  private isCancelling = false;
  private activeUploadPromises = new Map<number, Promise<void>>();

  async initiateOrResume(
    file: File,
    chunkSize: number = 4194304,
    concurrency: number = 4,
    onStateChange: (state: ChunkedUploadState) => void
  ): Promise<ChunkedUploadCommitResponse> {
    this.isPaused = false;
    this.isCancelling = false;
    this.abortController = new AbortController();

    const contentType = file.type || 'application/octet-stream';
    let initiation: ChunkedUploadInitiationResponse;
    let isResumed = false;

    // Check if an unexpired session exists in local storage
    const storedSession = this.storageService.getSession(file.name, file.size);
    if (storedSession && storedSession.chunkSize === chunkSize) {
      try {
        // Interrogate backend & Azure Storage to verify session still valid and staged blocks
        const blocksCheck = await this.apiService
          .getChunkedUploadBlocks(storedSession.uploadId)
          .toPromise();

        if (blocksCheck && blocksCheck.isResumable) {
          isResumed = true;
          initiation = {
            uploadId: storedSession.uploadId,
            blobName: storedSession.blobName,
            containerName: 'uploads',
            sasUrl: storedSession.sasUrl,
            chunkSize: storedSession.chunkSize,
            totalChunks: storedSession.totalChunks,
            blockIds: storedSession.blockIds,
            expiresAt: storedSession.expiresAt
          };
        } else {
          this.storageService.clearSession(file.name, file.size);
          initiation = await this.apiService
            .initiateChunkedUpload(file.name, file.size, contentType, chunkSize)
            .toPromise() as ChunkedUploadInitiationResponse;
        }
      } catch {
        // Fallback to fresh initiate
        initiation = await this.apiService
          .initiateChunkedUpload(file.name, file.size, contentType, chunkSize)
          .toPromise() as ChunkedUploadInitiationResponse;
      }
    } else {
      initiation = await this.apiService
        .initiateChunkedUpload(file.name, file.size, contentType, chunkSize)
        .toPromise() as ChunkedUploadInitiationResponse;
    }

    // Save session in local storage for resumability
    this.storageService.saveSession({
      uploadId: initiation.uploadId,
      blobName: initiation.blobName,
      fileName: file.name,
      fileSize: file.size,
      chunkSize: initiation.chunkSize,
      totalChunks: initiation.totalChunks,
      blockIds: initiation.blockIds,
      sasUrl: initiation.sasUrl,
      expiresAt: initiation.expiresAt,
      lastUpdated: Date.now()
    });

    // Build ChunkInfo array
    const chunks: ChunkInfo[] = [];
    for (let i = 0; i < initiation.totalChunks; i++) {
      const start = i * initiation.chunkSize;
      const end = Math.min(start + initiation.chunkSize, file.size);
      chunks.push({
        index: i,
        blockId: initiation.blockIds[i],
        startByte: start,
        endByte: end,
        size: end - start,
        status: 'queued',
        retryCount: 0
      });
    }

    // If resuming, query storage for already-staged blocks to mark them as staged
    if (isResumed) {
      try {
        const stagedInfo = await this.apiService.getChunkedUploadBlocks(initiation.uploadId).toPromise();
        if (stagedInfo && stagedInfo.stagedBlockIds) {
          const stagedSet = new Set(stagedInfo.stagedBlockIds);
          for (const chunk of chunks) {
            if (stagedSet.has(chunk.blockId)) {
              chunk.status = 'staged';
            }
          }
        }
      } catch (err) {
        console.warn('Could not retrieve staged block list during resumption, will upload from start.', err);
      }
    }

    const state: ChunkedUploadState = {
      uploadId: initiation.uploadId,
      fileName: file.name,
      fileSize: file.size,
      chunkSize: initiation.chunkSize,
      totalChunks: initiation.totalChunks,
      stagedChunks: chunks.filter(c => c.status === 'staged').length,
      status: 'Uploading',
      uploadedBytes: chunks.filter(c => c.status === 'staged').reduce((acc, c) => acc + c.size, 0),
      percentage: 0,
      speedBytesPerSec: 0,
      chunks: chunks,
      concurrency: concurrency,
      isResumed: isResumed
    };

    state.percentage = state.fileSize > 0
      ? Math.round((state.uploadedBytes / state.fileSize) * 10000) / 100
      : 0;

    onStateChange({ ...state, chunks: [...chunks] });

    // Azure Storage Client for staging blocks
    const blockBlobClient = new BlockBlobClient(initiation.sasUrl);

    // Speed calculation tracking
    let lastUploadedBytes = state.uploadedBytes;
    let lastTime = performance.now();

    // Execute upload worker pool with bounded concurrency
    await this.processQueue(
      file,
      chunks,
      blockBlobClient,
      concurrency,
      (updatedChunk) => {
        // Recalculate state on chunk status change
        const staged = chunks.filter(c => c.status === 'staged');
        state.stagedChunks = staged.length;
        state.uploadedBytes = staged.reduce((acc, c) => acc + c.size, 0);
        state.percentage = state.fileSize > 0
          ? Math.min(100, Math.round((state.uploadedBytes / state.fileSize) * 10000) / 100)
          : 100;

        const now = performance.now();
        const elapsed = (now - lastTime) / 1000;
        if (elapsed >= 0.5) {
          const diff = state.uploadedBytes - lastUploadedBytes;
          state.speedBytesPerSec = Math.max(0, diff / elapsed);
          lastUploadedBytes = state.uploadedBytes;
          lastTime = now;
        }

        onStateChange({ ...state, chunks: [...chunks] });
      }
    );

    if (this.isPaused) {
      state.status = 'Paused';
      state.speedBytesPerSec = 0;
      onStateChange({ ...state, chunks: [...chunks] });
      throw new Error('Upload paused.');
    }

    // Verify all chunks are staged
    const allStaged = chunks.every(c => c.status === 'staged');
    if (!allStaged) {
      throw new Error('Not all blocks were staged successfully.');
    }

    // All blocks staged -> commit block list through the Backend API
    state.status = 'Processing';
    state.speedBytesPerSec = 0;
    state.percentage = 100;
    onStateChange({ ...state, chunks: [...chunks] });

    const commitResult = await this.apiService
      .commitChunkedUpload(initiation.uploadId, initiation.blockIds)
      .toPromise();

    if (!commitResult) {
      throw new Error('Failed to commit uploaded block list on server.');
    }

    // Clear session storage on successful completion
    this.storageService.clearSession(file.name, file.size);

    state.status = 'Completed';
    onStateChange({ ...state, chunks: [...chunks] });

    return commitResult;
  }

  pause(): void {
    this.isPaused = true;
  }

  cancel(uploadId?: string, fileName?: string, fileSize?: number): void {
    if (this.abortController) {
      this.abortController.abort();
      this.abortController = null;
    }

    if (uploadId && !this.isCancelling) {
      this.isCancelling = true;
      this.apiService.cancelChunkedUpload(uploadId, 'Cancelled by user').subscribe({
        next: () => {
          this.isCancelling = false;
        },
        error: (e) => {
          this.isCancelling = false;
          console.warn('Cancel API failed', e);
        }
      });
    }

    if (fileName && fileSize) {
      this.storageService.clearSession(fileName, fileSize);
    }
  }

  private async processQueue(
    file: File,
    chunks: ChunkInfo[],
    blockBlobClient: BlockBlobClient,
    concurrency: number,
    onChunkUpdated: (chunk: ChunkInfo) => void
  ): Promise<void> {
    const queue = chunks.filter(c => c.status === 'queued' || c.status === 'failed');
    let currentIndex = 0;

    const worker = async (): Promise<void> => {
      while (currentIndex < queue.length) {
        if (this.isPaused || this.abortController?.signal.aborted) {
          return;
        }

        const chunk = queue[currentIndex++];
        await this.uploadChunkWithRetry(file, chunk, blockBlobClient, onChunkUpdated);
      }
    };

    // Spawn bounded concurrency workers
    const activeWorkers: Promise<void>[] = [];
    const workerCount = Math.min(concurrency, queue.length);

    for (let i = 0; i < workerCount; i++) {
      activeWorkers.push(worker());
    }

    await Promise.all(activeWorkers);
  }

  private async uploadChunkWithRetry(
    file: File,
    chunk: ChunkInfo,
    blockBlobClient: BlockBlobClient,
    onChunkUpdated: (chunk: ChunkInfo) => void,
    maxRetries: number = 4
  ): Promise<void> {
    const chunkBlob = file.slice(chunk.startByte, chunk.endByte);
    const chunkBuffer = await chunkBlob.arrayBuffer();

    while (chunk.retryCount <= maxRetries) {
      if (this.isPaused || this.abortController?.signal.aborted) {
        return;
      }

      chunk.status = 'uploading';
      onChunkUpdated(chunk);

      try {
        await blockBlobClient.stageBlock(
          chunk.blockId,
          chunkBuffer,
          chunkBuffer.byteLength,
          {
            abortSignal: this.abortController?.signal
          }
        );

        chunk.status = 'staged';
        chunk.errorMessage = undefined;
        onChunkUpdated(chunk);
        return;
      } catch (err: any) {
        if (this.abortController?.signal.aborted || this.isPaused) {
          return;
        }

        console.error(`stageBlock failed for block ${chunk.index}:`, err);
        chunk.retryCount++;
        chunk.status = 'failed';
        chunk.errorMessage = err?.message || 'Block upload failed.';
        onChunkUpdated(chunk);

        if (chunk.retryCount > maxRetries) {
          throw new Error(`Failed to upload block ${chunk.index} after ${maxRetries} retries: ${chunk.errorMessage}`);
        }

        // Exponential backoff with jitter: 2^attempt * 500ms + random(0-250ms)
        const delay = Math.min(10000, Math.pow(2, chunk.retryCount) * 500 + Math.random() * 250);
        console.warn(`Retry attempt ${chunk.retryCount} for block ${chunk.index} in ${Math.round(delay)}ms...`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }
}
