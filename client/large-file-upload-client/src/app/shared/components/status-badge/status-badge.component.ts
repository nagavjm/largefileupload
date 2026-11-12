import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="badgeClass">
      <span class="dot"></span>
      {{ status }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.25rem 0.625rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: capitalize;
    }
    .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
    }
    .badge-pending {
      background-color: #f1f5f9;
      color: #475569;
      .dot { background-color: #94a3b8; }
    }
    .badge-uploading {
      background-color: #eff6ff;
      color: #1d4ed8;
      .dot { background-color: #3b82f6; }
    }
    .badge-processing {
      background-color: #fffbeb;
      color: #b45309;
      .dot { background-color: #f59e0b; }
    }
    .badge-completed {
      background-color: #ecfdf5;
      color: #047857;
      .dot { background-color: #10b981; }
    }
    .badge-failed {
      background-color: #fef2f2;
      color: #b91c1c;
      .dot { background-color: #ef4444; }
    }
    .badge-cancelled {
      background-color: #f8fafc;
      color: #64748b;
      .dot { background-color: #94a3b8; }
    }
    .badge-expired {
      background-color: #fef2f2;
      color: #991b1b;
      .dot { background-color: #f87171; }
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status: string = 'Pending';

  get badgeClass(): string {
    switch (this.status?.toLowerCase()) {
      case 'completed': return 'badge-completed';
      case 'uploading': return 'badge-uploading';
      case 'processing': return 'badge-processing';
      case 'failed': return 'badge-failed';
      case 'cancelled': return 'badge-cancelled';
      case 'expired': return 'badge-expired';
      default: return 'badge-pending';
    }
  }
}
