import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileSizePipe } from '../../pipes/file-size.pipe';

@Component({
  selector: 'app-file-dropzone',
  standalone: true,
  imports: [CommonModule, FileSizePipe],
  template: `
    <div
      class="dropzone-box"
      [class.drag-over]="isDragOver"
      [class.has-file]="!!selectedFile"
      (dragover)="onDragOver($event)"
      (dragleave)="onDragLeave($event)"
      (drop)="onDrop($event)"
      (click)="fileInput.click()">

      <input
        #fileInput
        type="file"
        class="hidden-input"
        [disabled]="disabled"
        (change)="onFileSelected($event)" />

      <div class="dropzone-content" *ngIf="!selectedFile">
        <div class="icon-circle">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
            <polyline points="17 8 12 3 7 8"></polyline>
            <line x1="12" y1="3" x2="12" y2="15"></line>
          </svg>
        </div>
        <p class="primary-text">Click to choose a file or drag and drop here</p>
        <p class="secondary-text">Large files supported up to 50 GB (.zip, .mp4, .iso, .pdf, .csv, etc.)</p>
      </div>

      <div class="selected-file-info" *ngIf="selectedFile">
        <div class="file-icon">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
            <polyline points="14 2 14 8 20 8"></polyline>
            <line x1="16" y1="13" x2="8" y2="13"></line>
            <line x1="16" y1="17" x2="8" y2="17"></line>
            <polyline points="10 9 9 9 8 9"></polyline>
          </svg>
        </div>
        <div class="file-meta">
          <span class="file-name" [title]="selectedFile.name">{{ selectedFile.name }}</span>
          <span class="file-size">{{ selectedFile.size | fileSize }} • {{ selectedFile.type || 'application/octet-stream' }}</span>
        </div>
        <button
          type="button"
          class="remove-btn"
          *ngIf="!disabled"
          (click)="removeFile($event)">
          ✕
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dropzone-box {
      border: 2px dashed #cbd5e1;
      border-radius: 12px;
      padding: 2rem 1.5rem;
      text-align: center;
      background-color: #f8fafc;
      cursor: pointer;
      transition: all 0.2s ease-in-out;
    }
    .dropzone-box:hover {
      border-color: #3b82f6;
      background-color: #f0f7ff;
    }
    .dropzone-box.drag-over {
      border-color: #2563eb;
      background-color: #eff6ff;
      transform: scale(1.005);
    }
    .dropzone-box.has-file {
      border-style: solid;
      border-color: #e2e8f0;
      background-color: #ffffff;
      padding: 1.25rem;
    }
    .hidden-input {
      display: none;
    }
    .icon-circle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 56px;
      height: 56px;
      background-color: #e0f2fe;
      color: #0284c7;
      border-radius: 50%;
      margin-bottom: 0.875rem;
    }
    .primary-text {
      font-size: 0.9375rem;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 0.25rem 0;
    }
    .secondary-text {
      font-size: 0.8125rem;
      color: #64748b;
      margin: 0;
    }
    .selected-file-info {
      display: flex;
      align-items: center;
      gap: 1rem;
      text-align: left;
    }
    .file-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      background-color: #eff6ff;
      color: #3b82f6;
      border-radius: 8px;
      flex-shrink: 0;
    }
    .file-meta {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
    }
    .file-name {
      font-weight: 600;
      font-size: 0.9375rem;
      color: #1e293b;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .file-size {
      font-size: 0.8125rem;
      color: #64748b;
    }
    .remove-btn {
      background: none;
      border: 1px solid #cbd5e1;
      border-radius: 50%;
      width: 28px;
      height: 28px;
      cursor: pointer;
      color: #64748b;
      font-size: 0.875rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.15s;
    }
    .remove-btn:hover {
      background-color: #fee2e2;
      color: #dc2626;
      border-color: #fca5a5;
    }
  `]
})
export class FileDropzoneComponent {
  @Input() disabled = false;
  @Input() selectedFile: File | null = null;
  @Output() fileSelected = new EventEmitter<File | null>();

  isDragOver = false;

  onDragOver(event: DragEvent): void {
    if (this.disabled) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    if (this.disabled) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    if (this.disabled) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.fileSelected.emit(event.dataTransfer.files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (target.files && target.files.length > 0) {
      this.fileSelected.emit(target.files[0]);
    }
  }

  removeFile(event: Event): void {
    event.stopPropagation();
    this.selectedFile = null;
    this.fileSelected.emit(null);
  }
}
