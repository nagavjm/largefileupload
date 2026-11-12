import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { UploadSessionDto, PaginatedList } from '../../../core/models/upload.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';

@Component({
  selector: 'app-upload-history',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent, FileSizePipe],
  template: `
    <div class="history-card">
      <div class="card-header">
        <div class="title-group">
          <h3>Upload History</h3>
          <span class="count-pill">{{ totalCount }} uploads</span>
        </div>
        <button class="btn-refresh" (click)="loadHistory()" [disabled]="loading">
          <svg [class.spin]="loading" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10"></polyline>
            <polyline points="1 20 1 14 7 14"></polyline>
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
          </svg>
          Refresh
        </button>
      </div>

      <div class="table-container" *ngIf="sessions.length > 0">
        <table class="data-table">
          <thead>
            <tr>
              <th>File Name</th>
              <th>Strategy</th>
              <th>Size</th>
              <th>Progress</th>
              <th>Status</th>
              <th>Created</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of sessions">
              <td class="filename-cell">
                <span class="file-name" [title]="item.originalFileName">{{ item.originalFileName }}</span>
              </td>
              <td>
                <span class="strategy-badge" [class.chunked]="item.uploadStrategy === 'ManualBlockBlob'">
                  {{ item.uploadStrategy === 'DirectSas' ? 'Direct SAS' : 'Block Blob' }}
                </span>
              </td>
              <td class="size-cell">{{ item.fileSize | fileSize }}</td>
              <td>
                <span class="progress-text">{{ item.progressPercentage }}%</span>
              </td>
              <td>
                <app-status-badge [status]="item.status"></app-status-badge>
              </td>
              <td class="time-cell">{{ item.createdAt | date:'short' }}</td>
              <td>
                <button
                  class="btn-delete"
                  title="Delete record and blob"
                  (click)="deleteSession(item.id)">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="empty-state" *ngIf="sessions.length === 0 && !loading">
        <p>No upload sessions found. Start an upload above!</p>
      </div>

      <div class="loading-state" *ngIf="loading && sessions.length === 0">
        <p>Loading history...</p>
      </div>
    </div>
  `,
  styles: [`
    .history-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.5rem;
      margin-top: 2rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.25rem;
    }
    .title-group {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .title-group h3 {
      font-size: 1.125rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0;
    }
    .count-pill {
      background-color: #f1f5f9;
      color: #475569;
      padding: 0.2rem 0.5rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
    }
    .btn-refresh {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.4rem 0.75rem;
      font-size: 0.8125rem;
      font-weight: 600;
      color: #475569;
      cursor: pointer;
      transition: all 0.15s;
    }
    .btn-refresh:hover:not(:disabled) {
      background: #f1f5f9;
      color: #0f172a;
    }
    .spin {
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }
    .table-container {
      overflow-x: auto;
    }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
      font-size: 0.875rem;
    }
    .data-table th {
      background-color: #f8fafc;
      color: #64748b;
      font-weight: 600;
      padding: 0.75rem 1rem;
      border-bottom: 1px solid #e2e8f0;
      text-transform: uppercase;
      font-size: 0.6875rem;
      letter-spacing: 0.05em;
    }
    .data-table td {
      padding: 0.875rem 1rem;
      border-bottom: 1px solid #f1f5f9;
      color: #334155;
    }
    .data-table tr:hover td {
      background-color: #f8fafc;
    }
    .filename-cell {
      max-width: 220px;
    }
    .file-name {
      display: block;
      font-weight: 600;
      color: #1e293b;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .strategy-badge {
      display: inline-block;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      background-color: #e0f2fe;
      color: #0369a1;
    }
    .strategy-badge.chunked {
      background-color: #fef3c7;
      color: #92400e;
    }
    .size-cell {
      white-space: nowrap;
      color: #64748b;
    }
    .progress-text {
      font-weight: 600;
      color: #0f172a;
    }
    .time-cell {
      white-space: nowrap;
      font-size: 0.8125rem;
      color: #64748b;
    }
    .btn-delete {
      background: none;
      border: 1px solid transparent;
      padding: 0.35rem;
      border-radius: 6px;
      cursor: pointer;
      color: #94a3b8;
      display: flex;
      align-items: center;
      transition: all 0.15s;
    }
    .btn-delete:hover {
      color: #ef4444;
      background-color: #fee2e2;
      border-color: #fca5a5;
    }
    .empty-state, .loading-state {
      text-align: center;
      padding: 2.5rem 1rem;
      color: #94a3b8;
      font-size: 0.875rem;
    }
  `]
})
export class UploadHistoryComponent implements OnInit {
  private readonly apiService = inject(ApiService);

  sessions: UploadSessionDto[] = [];
  totalCount = 0;
  loading = false;

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.apiService.getUploadHistory(1, 20).subscribe({
      next: (res) => {
        this.sessions = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  deleteSession(id: string): void {
    if (!confirm('Are you sure you want to delete this upload session?')) return;

    this.apiService.deleteUploadSession(id).subscribe({
      next: () => {
        this.sessions = this.sessions.filter(s => s.id !== id);
        this.totalCount = Math.max(0, this.totalCount - 1);
      }
    });
  }
}
