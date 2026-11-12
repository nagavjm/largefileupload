import { Component, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BlockOrchestratorService } from '../services/block-orchestrator.service';
import { ChunkedStorageService } from '../services/chunked-storage.service';
import { ChunkedUploadState, ChunkInfo } from '../models/chunk.model';
import { ChunkedUploadCommitResponse } from '../../../core/models/upload.model';
import { FileDropzoneComponent } from '../../../shared/components/file-dropzone/file-dropzone.component';
import { ProgressBarComponent } from '../../../shared/components/progress-bar/progress-bar.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { UploadHistoryComponent } from '../../upload-history/components/upload-history.component';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';

@Component({
  selector: 'app-chunked-upload',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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
          <h2>Chunked Upload / Block Blob</h2>
          <p class="subtitle">Manual chunk-based upload with parallel processing, exponential backoff, and resumability</p>
        </div>
        <div class="architecture-badge">
          <span>Angular Slices Chunks → stageBlock → Backend commitBlockList</span>
        </div>
      </div>

      <!-- Config & Upload Options Card -->
      <div class="config-card">
        <div class="config-header">
          <h3>Upload Parameters</h3>
          <span class="config-hint">Tweak chunk size & parallel workers to observe throughput</span>
        </div>

        <div class="config-controls">
          <div class="control-group">
            <label for="chunkSizeSelect">Chunk Size:</label>
            <select
              id="chunkSizeSelect"
              [(ngModel)]="selectedChunkSize"
              [disabled]="isUploading || isPaused">
              <option [ngValue]="2097152">2 MB</option>
              <option [ngValue]="4194304">4 MB (Default)</option>
              <option [ngValue]="8388608">8 MB</option>
              <option [ngValue]="16777216">16 MB</option>
            </select>
          </div>

          <div class="control-group">
            <label for="concurrencySelect">Parallel Workers (Concurrency):</label>
            <select
              id="concurrencySelect"
              [(ngModel)]="concurrency"
              [disabled]="isUploading || isPaused">
              <option [ngValue]="2">2 Workers</option>
              <option [ngValue]="4">4 Workers (Default)</option>
              <option [ngValue]="6">6 Workers</option>
              <option [ngValue]="8">8 Workers</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Upload Card -->
      <div class="upload-card">
        <app-file-dropzone
          [disabled]="isUploading"
          [selectedFile]="selectedFile"
          (fileSelected)="onFileSelected($event)">
        </app-file-dropzone>

        <!-- Resumption Alert Banner -->
        <div class="alert alert-info" *ngIf="hasExistingSession && !isUploading && !isCompleted">
          <div class="alert-icon">ℹ</div>
          <div class="alert-content">
            <h4>Existing Upload Session Found!</h4>
            <p>An interrupted session for this file exists. Click "Resume Upload" to continue without re-uploading completed chunks.</p>
          </div>
        </div>

        <!-- Controls & State -->
        <div class="upload-section" *ngIf="selectedFile">
          <div class="action-bar">
            <!-- Start / Resume Button -->
            <button
              class="btn btn-primary"
              *ngIf="!isUploading && !isPaused && !isCompleted"
              (click)="startUpload()">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="17 8 12 3 7 8"></polyline>
                <line x1="12" y1="3" x2="12" y2="15"></line>
              </svg>
              {{ hasExistingSession ? 'Resume Upload' : 'Start Chunked Upload' }}
            </button>

            <!-- Pause Button -->
            <button
              class="btn btn-warning"
              *ngIf="isUploading"
              (click)="pauseUpload()">
              Pause Upload
            </button>

            <!-- Resume Button -->
            <button
              class="btn btn-primary"
              *ngIf="isPaused"
              (click)="resumeUpload()">
              Resume Upload
            </button>

            <!-- Cancel Button -->
            <button
              class="btn btn-danger"
              *ngIf="isUploading || isPaused"
              (click)="cancelUpload()">
              Cancel
            </button>

            <!-- Retry Button -->
            <button
              class="btn btn-secondary"
              *ngIf="isFailed"
              (click)="startUpload()">
              Retry Upload
            </button>

            <div class="status-indicator">
              <span class="label">Status:</span>
              <app-status-badge [status]="state.status"></app-status-badge>
            </div>
          </div>

          <!-- Progress Bar Component -->
          <app-progress-bar
            *ngIf="state.status !== 'Idle'"
            [percentage]="state.percentage"
            [uploadedBytes]="state.uploadedBytes"
            [totalBytes]="state.fileSize"
            [speedBytesPerSec]="state.speedBytesPerSec"
            [status]="state.status">
          </app-progress-bar>

          <!-- Real-Time Metrics Badges -->
          <div class="metrics-grid" *ngIf="state.totalChunks > 0">
            <div class="metric-card">
              <span class="metric-label">Staged Blocks</span>
              <span class="metric-val">{{ state.stagedChunks }} / {{ state.totalChunks }}</span>
            </div>
            <div class="metric-card">
              <span class="metric-label">Block Size</span>
              <span class="metric-val">{{ state.chunkSize | fileSize }}</span>
            </div>
            <div class="metric-card">
              <span class="metric-label">Active Workers</span>
              <span class="metric-val">{{ concurrency }} parallel</span>
            </div>
            <div class="metric-card">
              <span class="metric-label">Resumed Mode</span>
              <span class="metric-val">{{ state.isResumed ? 'Yes (Skipped Blocks)' : 'No (Fresh)' }}</span>
            </div>
          </div>

          <!-- Interactive Chunk Block Visualizer Matrix -->
          <div class="chunk-matrix-container" *ngIf="state.chunks.length > 0">
            <div class="matrix-header">
              <span class="matrix-title">Block Blob Visual Matrix ({{ state.chunks.length }} total blocks)</span>
              <div class="matrix-legend">
                <span class="legend-item"><span class="block-preview staged"></span> Staged</span>
                <span class="legend-item"><span class="block-preview uploading"></span> Staging</span>
                <span class="legend-item"><span class="block-preview queued"></span> Queued</span>
                <span class="legend-item"><span class="block-preview failed"></span> Retrying</span>
              </div>
            </div>

            <div class="chunk-grid">
              <div
                *ngFor="let chunk of displayedChunks"
                class="chunk-cell"
                [ngClass]="'cell-' + chunk.status"
                [title]="getChunkTooltip(chunk)">
              </div>
            </div>
            <span class="overflow-text" *ngIf="state.chunks.length > 300">
              Showing first 300 of {{ state.chunks.length }} blocks in visualizer
            </span>
          </div>
        </div>

        <!-- Success Notification Box -->
        <div class="alert alert-success" *ngIf="completionResult">
          <div class="alert-icon">✓</div>
          <div class="alert-content">
            <h4>All Blocks Committed Successfully!</h4>
            <p>Azure Block Blob committed via backend <code>commitBlockList</code> API.</p>
            <div class="completion-details">
              <span><strong>Blocks Committed:</strong> {{ completionResult.totalBlocksCommitted }}</span>
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
            <h4>Upload Error</h4>
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
      background-color: #fef3c7;
      border: 1px solid #fde68a;
      color: #92400e;
      font-size: 0.8125rem;
      font-weight: 600;
      padding: 0.4rem 0.8rem;
      border-radius: 9999px;
      white-space: nowrap;
    }
    .config-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.25rem 1.75rem;
      margin-bottom: 1.5rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }
    .config-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.875rem;
    }
    .config-header h3 {
      font-size: 0.9375rem;
      font-weight: 700;
      color: #1e293b;
      margin: 0;
    }
    .config-hint {
      font-size: 0.75rem;
      color: #64748b;
    }
    .config-controls {
      display: flex;
      gap: 2rem;
      flex-wrap: wrap;
    }
    .control-group {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .control-group label {
      font-size: 0.875rem;
      color: #475569;
      font-weight: 500;
    }
    .control-group select {
      padding: 0.4rem 0.75rem;
      border-radius: 6px;
      border: 1px solid #cbd5e1;
      font-size: 0.875rem;
      color: #1e293b;
      background: #ffffff;
      cursor: pointer;
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
    .btn-primary { background-color: #2563eb; color: #ffffff; }
    .btn-primary:hover { background-color: #1d4ed8; }
    .btn-warning { background-color: #f59e0b; color: #ffffff; }
    .btn-warning:hover { background-color: #d97706; }
    .btn-danger { background-color: #ef4444; color: #ffffff; }
    .btn-danger:hover { background-color: #dc2626; }
    .btn-secondary { background-color: #f1f5f9; color: #334155; border: 1px solid #cbd5e1; }
    .btn-secondary:hover { background-color: #e2e8f0; }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1rem;
      margin: 1.25rem 0;
    }
    .metric-card {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.75rem 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .metric-label {
      font-size: 0.75rem;
      font-weight: 600;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .metric-val {
      font-size: 1.125rem;
      font-weight: 700;
      color: #0f172a;
    }

    .chunk-matrix-container {
      margin-top: 1.5rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 1rem;
    }
    .matrix-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    .matrix-title {
      font-size: 0.8125rem;
      font-weight: 700;
      color: #334155;
    }
    .matrix-legend {
      display: flex;
      gap: 1rem;
      font-size: 0.75rem;
      color: #64748b;
    }
    .legend-item {
      display: flex;
      align-items: center;
      gap: 0.375rem;
    }
    .block-preview {
      width: 10px;
      height: 10px;
      border-radius: 2px;
      display: inline-block;
    }
    .block-preview.staged { background-color: #10b981; }
    .block-preview.uploading { background-color: #3b82f6; }
    .block-preview.queued { background-color: #cbd5e1; }
    .block-preview.failed { background-color: #ef4444; }

    .chunk-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(14px, 1fr));
      gap: 3px;
      max-height: 160px;
      overflow-y: auto;
      padding: 4px;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
    }
    .chunk-cell {
      height: 14px;
      border-radius: 2px;
      transition: background-color 0.2s ease;
      cursor: pointer;
    }
    .cell-staged { background-color: #10b981; }
    .cell-uploading { background-color: #3b82f6; animation: pulse 1s infinite alternate; }
    .cell-queued { background-color: #e2e8f0; }
    .cell-failed { background-color: #ef4444; }

    @keyframes pulse {
      from { opacity: 0.6; }
      to { opacity: 1; }
    }
    .overflow-text {
      font-size: 0.6875rem;
      color: #94a3b8;
      margin-top: 0.375rem;
      display: block;
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
    .alert-content h4 { margin: 0 0 0.25rem 0; font-size: 0.9375rem; font-weight: 700; }
    .alert-content p { margin: 0 0 0.5rem 0; font-size: 0.875rem; }
    .completion-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      font-size: 0.8125rem;
      color: #065f46;
    }
    .blob-url { word-break: break-all; }
    .alert-success { background-color: #ecfdf5; border: 1px solid #a7f3d0; color: #065f46; .alert-icon { background: #10b981; color: white; } }
    .alert-danger { background-color: #fef2f2; border: 1px solid #fecaca; color: #991b1b; .alert-icon { background: #ef4444; color: white; } }
    .alert-info { background-color: #eff6ff; border: 1px solid #bfdbfe; color: #1e40af; .alert-icon { background: #3b82f6; color: white; } }
  `]
})
export class ChunkedUploadComponent {
  private readonly orchestrator = inject(BlockOrchestratorService);
  private readonly storageService = inject(ChunkedStorageService);

  @ViewChild('historyComponent') historyComponent?: UploadHistoryComponent;

  selectedFile: File | null = null;
  selectedChunkSize: number = 4194304; // 4 MB default
  concurrency: number = 4;
  hasExistingSession = false;

  state: ChunkedUploadState = {
    fileName: '',
    fileSize: 0,
    chunkSize: 4194304,
    totalChunks: 0,
    stagedChunks: 0,
    status: 'Idle',
    uploadedBytes: 0,
    percentage: 0,
    speedBytesPerSec: 0,
    chunks: [],
    concurrency: 4,
    isResumed: false
  };

  completionResult: ChunkedUploadCommitResponse | null = null;
  errorMessage: string | null = null;

  get isUploading(): boolean {
    return this.state.status === 'Uploading' || this.state.status === 'Processing';
  }

  get isPaused(): boolean {
    return this.state.status === 'Paused';
  }

  get isCompleted(): boolean {
    return this.state.status === 'Completed';
  }

  get isFailed(): boolean {
    return this.state.status === 'Failed';
  }

  get displayedChunks(): ChunkInfo[] {
    return this.state.chunks.slice(0, 300);
  }

  onFileSelected(file: File | null): void {
    this.selectedFile = file;
    this.completionResult = null;
    this.errorMessage = null;

    if (file) {
      const existing = this.storageService.getSession(file.name, file.size);
      this.hasExistingSession = !!existing;
      if (existing) {
        this.selectedChunkSize = existing.chunkSize;
      }
    } else {
      this.hasExistingSession = false;
    }

    this.state = {
      fileName: file ? file.name : '',
      fileSize: file ? file.size : 0,
      chunkSize: this.selectedChunkSize,
      totalChunks: file ? Math.ceil(file.size / this.selectedChunkSize) : 0,
      stagedChunks: 0,
      status: 'Idle',
      uploadedBytes: 0,
      percentage: 0,
      speedBytesPerSec: 0,
      chunks: [],
      concurrency: this.concurrency,
      isResumed: false
    };
  }

  async startUpload(): Promise<void> {
    if (!this.selectedFile || this.isUploading) return;

    this.completionResult = null;
    this.errorMessage = null;

    try {
      this.completionResult = await this.orchestrator.initiateOrResume(
        this.selectedFile,
        this.selectedChunkSize,
        this.concurrency,
        (updatedState) => {
          this.state = { ...updatedState };
        }
      );

      this.hasExistingSession = false;
      this.historyComponent?.loadHistory();
    } catch (err: any) {
      if (err.message === 'Upload paused.') {
        this.state.status = 'Paused';
      } else {
        this.state.status = 'Failed';
        this.errorMessage = this.extractErrorMessage(err);
      }
    }
  }

  private extractErrorMessage(err: any): string {
    if (err?.error?.errors && typeof err.error.errors === 'object') {
      const messages = Object.values(err.error.errors).flat();
      if (messages.length > 0) return messages.join(' ');
    }
    if (err?.error?.detail) return err.error.detail;
    if (err?.error?.title) return err.error.title;
    if (typeof err?.error === 'string') return err.error;
    return err?.message || 'Chunked upload failed.';
  }

  pauseUpload(): void {
    this.orchestrator.pause();
    this.state.status = 'Paused';
  }

  resumeUpload(): void {
    this.startUpload();
  }

  cancelUpload(): void {
    this.orchestrator.cancel(
      this.state.uploadId,
      this.selectedFile?.name,
      this.selectedFile?.size
    );
    this.state.status = 'Cancelled';
    this.hasExistingSession = false;
    this.errorMessage = 'Upload was cancelled.';
  }

  getChunkTooltip(chunk: ChunkInfo): string {
    return `Block #${chunk.index} (${(chunk.size / 1024 / 1024).toFixed(2)} MB)\nStatus: ${chunk.status}\nRange: ${chunk.startByte} - ${chunk.endByte}`;
  }
}
