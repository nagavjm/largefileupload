import { Component, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DirectUploadService } from '../services/direct-upload.service';
import { UploadProgressState, CompleteDirectUploadResponse } from '../../../core/models/upload.model';
import { FileDropzoneComponent } from '../../../shared/components/file-dropzone/file-dropzone.component';
import { ProgressBarComponent } from '../../../shared/components/progress-bar/progress-bar.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { UploadHistoryComponent } from '../../upload-history/components/upload-history.component';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';

@Component({
  selector: 'app-direct-upload',
  standalone: true,
  imports: [
    CommonModule,
    FileDropzoneComponent,
    ProgressBarComponent,
    StatusBadgeComponent,
    UploadHistoryComponent,
    FileSizePipe
  ],
  template: `
    <div class="page-container">
      <div class="module-header">
        <div>
          <h2>Industry-Standard Techniques</h2>
          <p class="subtitle">Direct-to-Blob Upload using short-lived User Delegation SAS</p>
        </div>
        <div class="architecture-badge">
          <span>Angular → SAS Token → Direct to Azure Storage</span>
        </div>
      </div>

      <!-- Upload Card -->
      <div class="upload-card">
        <app-file-dropzone
          [disabled]="isUploading"
          [selectedFile]="selectedFile"
          (fileSelected)="onFileSelected($event)">
        </app-file-dropzone>

        <!-- Upload Controls & Progress -->
        <div class="upload-section" *ngIf="selectedFile">
          <div class="action-bar">
            <button
              class="btn btn-primary"
              [disabled]="isUploading || isCompleted"
              (click)="startUpload()">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="17 8 12 3 7 8"></polyline>
                <line x1="12" y1="3" x2="12" y2="15"></line>
              </svg>
              {{ isUploading ? 'Uploading...' : 'Start Upload' }}
            </button>

            <button
              class="btn btn-danger"
              *ngIf="isUploading"
              (click)="cancelUpload()">
              Cancel
            </button>

            <button
              class="btn btn-secondary"
              *ngIf="isFailed || isCancelled"
              (click)="retryUpload()">
              Retry Upload
            </button>

            <div class="status-indicator">
              <span class="label">Status:</span>
              <app-status-badge [status]="progress.status"></app-status-badge>
            </div>
          </div>

          <!-- Progress Bar Component -->
          <app-progress-bar
            *ngIf="progress.status !== 'Idle'"
            [percentage]="progress.percentage"
            [uploadedBytes]="progress.uploadedBytes"
            [totalBytes]="progress.fileSize"
            [speedBytesPerSec]="progress.speedBytesPerSec"
            [status]="progress.status">
          </app-progress-bar>
        </div>

        <!-- Success Notification Box -->
        <div class="alert alert-success" *ngIf="completionResult">
          <div class="alert-icon">✓</div>
          <div class="alert-content">
            <h4>Upload Completed Successfully!</h4>
            <p>File verified and saved to Azure Blob Storage.</p>
            <div class="completion-details">
              <span><strong>Verified Bytes:</strong> {{ completionResult.verifiedBytes | fileSize }}</span>
              <span><strong>Completed At:</strong> {{ completionResult.completedAt | date:'medium' }}</span>
              <span class="blob-url" [title]="completionResult.blobUri">
                <strong>Storage URI:</strong> {{ completionResult.blobUri }}
              </span>
            </div>
          </div>
        </div>

        <!-- Error Notification Box -->
        <div class="alert alert-danger" *ngIf="errorMessage">
          <div class="alert-icon">✕</div>
          <div class="alert-content">
            <h4>Upload Failed</h4>
            <p>{{ errorMessage }}</p>
          </div>
        </div>
      </div>

      <!-- Upload History Component -->
      <app-upload-history #historyComponent></app-upload-history>
    </div>
  `,
  styles: [`
    .page-container {
      max-width: 1000px;
      margin: 0 auto;
    }
    .module-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
      gap: 1rem;
    }
    .module-header h2 {
      font-size: 1.5rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 0.25rem 0;
    }
    .subtitle {
      color: #64748b;
      margin: 0;
      font-size: 0.9375rem;
    }
    .architecture-badge {
      background-color: #eff6ff;
      border: 1px solid #bfdbfe;
      color: #1d4ed8;
      font-size: 0.8125rem;
      font-weight: 600;
      padding: 0.4rem 0.8rem;
      border-radius: 9999px;
      white-space: nowrap;
    }
    .upload-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.75rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }
    .upload-section {
      margin-top: 1.5rem;
      padding-top: 1.25rem;
      border-top: 1px solid #f1f5f9;
    }
    .action-bar {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }
    .status-indicator {
      margin-left: auto;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .status-indicator .label {
      font-size: 0.875rem;
      color: #64748b;
      font-weight: 500;
    }
    .btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1.25rem;
      border-radius: 8px;
      font-weight: 600;
      font-size: 0.875rem;
      cursor: pointer;
      border: none;
      transition: all 0.15s ease-in-out;
    }
    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .btn-primary {
      background-color: #2563eb;
      color: #ffffff;
    }
    .btn-primary:hover:not(:disabled) {
      background-color: #1d4ed8;
    }
    .btn-danger {
      background-color: #ef4444;
      color: #ffffff;
    }
    .btn-danger:hover {
      background-color: #dc2626;
    }
    .btn-secondary {
      background-color: #f1f5f9;
      color: #334155;
      border: 1px solid #cbd5e1;
    }
    .btn-secondary:hover {
      background-color: #e2e8f0;
    }
    .alert {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      border-radius: 8px;
      margin-top: 1.5rem;
    }
    .alert-icon {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      flex-shrink: 0;
    }
    .alert-content h4 {
      margin: 0 0 0.25rem 0;
      font-size: 0.9375rem;
      font-weight: 700;
    }
    .alert-content p {
      margin: 0 0 0.5rem 0;
      font-size: 0.875rem;
    }
    .completion-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      font-size: 0.8125rem;
      color: #065f46;
    }
    .blob-url {
      word-break: break-all;
    }
    .alert-success {
      background-color: #ecfdf5;
      border: 1px solid #a7f3d0;
      color: #065f46;
      .alert-icon { background: #10b981; color: white; }
    }
    .alert-danger {
      background-color: #fef2f2;
      border: 1px solid #fecaca;
      color: #991b1b;
      .alert-icon { background: #ef4444; color: white; }
    }
  `]
})
export class DirectUploadComponent {
  private readonly directUploadService = inject(DirectUploadService);

  @ViewChild('historyComponent') historyComponent?: UploadHistoryComponent;

  selectedFile: File | null = null;
  abortController: AbortController | null = null;

  progress: UploadProgressState = {
    fileName: '',
    fileSize: 0,
    uploadedBytes: 0,
    percentage: 0,
    status: 'Idle',
    speedBytesPerSec: 0
  };

  completionResult: CompleteDirectUploadResponse | null = null;
  errorMessage: string | null = null;

  get isUploading(): boolean {
    return this.progress.status === 'Uploading' || this.progress.status === 'Processing';
  }

  get isCompleted(): boolean {
    return this.progress.status === 'Completed';
  }

  get isFailed(): boolean {
    return this.progress.status === 'Failed';
  }

  get isCancelled(): boolean {
    return this.progress.status === 'Cancelled';
  }

  onFileSelected(file: File | null): void {
    this.selectedFile = file;
    this.completionResult = null;
    this.errorMessage = null;
    this.progress = {
      fileName: file ? file.name : '',
      fileSize: file ? file.size : 0,
      uploadedBytes: 0,
      percentage: 0,
      status: 'Idle',
      speedBytesPerSec: 0
    };
  }

  async startUpload(): Promise<void> {
    if (!this.selectedFile || this.isUploading) return;

    this.completionResult = null;
    this.errorMessage = null;
    this.abortController = new AbortController();

    try {
      this.completionResult = await this.directUploadService.uploadFile(
        this.selectedFile,
        (updatedState) => {
          this.progress = { ...updatedState };
        },
        this.abortController.signal
      );

      // Refresh history
      this.historyComponent?.loadHistory();
    } catch (err: any) {
      if (err.name === 'AbortError') {
        this.progress.status = 'Cancelled';
        this.errorMessage = 'Upload was cancelled.';
      } else {
        this.progress.status = 'Failed';
        this.errorMessage = err?.error?.detail || err?.message || 'Direct upload to Azure Blob Storage failed.';
      }
    } finally {
      this.abortController = null;
    }
  }

  cancelUpload(): void {
    if (this.abortController) {
      this.abortController.abort();
    }
  }

  retryUpload(): void {
    this.startUpload();
  }
}
