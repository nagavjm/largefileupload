import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileSizePipe } from '../../pipes/file-size.pipe';

@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [CommonModule, FileSizePipe],
  template: `
    <div class="progress-container">
      <div class="progress-header">
        <div class="progress-stats">
          <span class="percentage">{{ percentage | number:'1.1-2' }}%</span>
          <span class="bytes" *ngIf="totalBytes > 0">
            {{ uploadedBytes | fileSize }} of {{ totalBytes | fileSize }}
          </span>
        </div>
        <div class="speed-stat" *ngIf="speedBytesPerSec > 0 && status === 'Uploading'">
          <span class="speed">{{ speedBytesPerSec | fileSize }}/s</span>
        </div>
      </div>

      <div class="progress-track">
        <div
          class="progress-fill"
          [style.width.%]="percentage"
          [ngClass]="{
            'fill-uploading': status === 'Uploading',
            'fill-completed': status === 'Completed',
            'fill-failed': status === 'Failed' || status === 'Cancelled',
            'fill-processing': status === 'Processing'
          }">
        </div>
      </div>
    </div>
  `,
  styles: [`
    .progress-container {
      width: 100%;
      margin: 1.25rem 0;
    }
    .progress-header {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
    }
    .progress-stats {
      display: flex;
      gap: 0.75rem;
      align-items: baseline;
    }
    .percentage {
      font-weight: 700;
      font-size: 1.125rem;
      color: #1e293b;
    }
    .bytes {
      color: #64748b;
    }
    .speed-stat {
      color: #0284c7;
      font-weight: 600;
      font-size: 0.8125rem;
    }
    .progress-track {
      width: 100%;
      height: 10px;
      background-color: #e2e8f0;
      border-radius: 9999px;
      overflow: hidden;
      box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.05);
    }
    .progress-fill {
      height: 100%;
      transition: width 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      border-radius: 9999px;
    }
    .fill-uploading {
      background: linear-gradient(90deg, #3b82f6, #0284c7);
    }
    .fill-completed {
      background: linear-gradient(90deg, #10b981, #059669);
    }
    .fill-processing {
      background: linear-gradient(90deg, #f59e0b, #d97706);
    }
    .fill-failed {
      background: linear-gradient(90deg, #ef4444, #dc2626);
    }
  `]
})
export class ProgressBarComponent {
  @Input() percentage: number = 0;
  @Input() uploadedBytes: number = 0;
  @Input() totalBytes: number = 0;
  @Input() speedBytesPerSec: number = 0;
  @Input() status: string = 'Idle';
}
