import { Injectable, inject } from '@angular/core';
import { BlockBlobClient } from '@azure/storage-blob';
import { ApiService } from '../../../core/services/api.service';
import {
  UploadProgressState,
  CompleteDirectUploadResponse
} from '../../../core/models/upload.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DirectUploadService {
  private readonly apiService = inject(ApiService);

  async uploadFile(
    file: File,
    onProgressUpdate: (state: UploadProgressState) => void,
    abortSignal?: AbortSignal
  ): Promise<CompleteDirectUploadResponse> {
    const contentType = file.type || 'application/octet-stream';

    // 1. Initial State
    const progressState: UploadProgressState = {
      fileName: file.name,
      fileSize: file.size,
      uploadedBytes: 0,
      percentage: 0,
      status: 'Pending',
      speedBytesPerSec: 0
    };
    onProgressUpdate({ ...progressState });

    // 2. Initiate Upload Session with Backend
    const initiation = await this.apiService
      .initiateDirectUpload(file.name, file.size, contentType)
      .toPromise();

    if (!initiation) {
      throw new Error('Failed to initiate upload session with server.');
    }

    progressState.uploadId = initiation.uploadId;
    progressState.status = 'Uploading';
    onProgressUpdate({ ...progressState });

    // 3. Create Azure Storage SDK BlockBlobClient using the SAS URL
    const blockBlobClient = new BlockBlobClient(initiation.sasUrl);

    // Track speed metrics
    let lastLoaded = 0;
    let lastTime = performance.now();
    const blockSize = initiation.maxBlockSizeBytes || 8 * 1024 * 1024; // 8 MB chunks for SDK

    // 4. Direct Upload to Azure Blob Storage via SDK uploadBrowserData
    await blockBlobClient.uploadBrowserData(file, {
      blockSize: blockSize,
      concurrency: 4,
      abortSignal: abortSignal,
      blobHTTPHeaders: {
        blobContentType: contentType
      },
      onProgress: (ev) => {
        const now = performance.now();
        const timeDiff = (now - lastTime) / 1000;

        if (timeDiff >= 0.5) {
          const bytesDiff = ev.loadedBytes - lastLoaded;
          progressState.speedBytesPerSec = Math.max(0, bytesDiff / timeDiff);
          lastLoaded = ev.loadedBytes;
          lastTime = now;
        }

        progressState.uploadedBytes = ev.loadedBytes;
        progressState.percentage = file.size > 0
          ? Math.min(100, Math.round((ev.loadedBytes / file.size) * 10000) / 100)
          : 100;

        onProgressUpdate({ ...progressState });
      }
    });

    // 5. Direct upload to Azure Blob Storage complete -> Call backend to verify and finalize
    progressState.status = 'Processing';
    progressState.percentage = 100;
    progressState.uploadedBytes = file.size;
    onProgressUpdate({ ...progressState });

    const completion = await this.apiService
      .completeDirectUpload(initiation.uploadId)
      .toPromise();

    if (!completion) {
      throw new Error('Upload finished in storage, but backend verification failed.');
    }

    progressState.status = 'Completed';
    onProgressUpdate({ ...progressState });

    return completion;
  }
}
