import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="sidebar">
      <div class="sidebar-brand">
        <div class="logo-box">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <polyline points="16 16 12 12 8 16"></polyline>
            <line x1="12" y1="12" x2="12" y2="21"></line>
            <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3"></path>
            <polyline points="16 16 12 12 8 16"></polyline>
          </svg>
        </div>
        <div class="brand-text">
          <span class="brand-title">Azure Large File</span>
          <span class="brand-sub">Upload Platform</span>
        </div>
      </div>

      <nav class="nav-menu">
        <div class="menu-label">UPLOAD STRATEGIES</div>

        <a
          routerLink="/direct-upload"
          routerLinkActive="active"
          class="nav-item">
          <div class="nav-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
              <polyline points="17 8 12 3 7 8"></polyline>
              <line x1="12" y1="3" x2="12" y2="15"></line>
            </svg>
          </div>
          <div class="nav-label">
            <span class="primary">Industry-Standard</span>
            <span class="secondary">Direct SAS Upload</span>
          </div>
        </a>

        <a
          routerLink="/chunked-upload"
          routerLinkActive="active"
          class="nav-item">
          <div class="nav-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="2" y="2" width="20" height="8" rx="2" ry="2"></rect>
              <rect x="2" y="14" width="20" height="8" rx="2" ry="2"></rect>
              <line x1="6" y1="6" x2="6.01" y2="6"></line>
              <line x1="6" y1="18" x2="6.01" y2="18"></line>
            </svg>
          </div>
          <div class="nav-label">
            <span class="primary">Chunked Upload</span>
            <span class="secondary">Block Blob & Resumability</span>
          </div>
        </a>
      </nav>

      <div class="sidebar-footer">
        <div class="cloud-status">
          <span class="status-dot"></span>
          <span class="cloud-text">Azure Blob Connected</span>
        </div>
      </div>
    </aside>
  `,
  styles: [`
    .sidebar {
      width: 280px;
      height: 100vh;
      background-color: #0f172a;
      color: #f8fafc;
      display: flex;
      flex-direction: column;
      flex-shrink: 0;
      border-right: 1px solid #1e293b;
    }
    .sidebar-brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1.5rem 1.25rem;
      border-bottom: 1px solid #1e293b;
    }
    .logo-box {
      width: 38px;
      height: 38px;
      background: linear-gradient(135deg, #0284c7, #2563eb);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #ffffff;
      box-shadow: 0 2px 4px rgba(37, 99, 235, 0.3);
    }
    .brand-text {
      display: flex;
      flex-direction: column;
    }
    .brand-title {
      font-weight: 700;
      font-size: 0.9375rem;
      color: #f8fafc;
    }
    .brand-sub {
      font-size: 0.75rem;
      color: #94a3b8;
    }
    .nav-menu {
      padding: 1.25rem 0.75rem;
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .menu-label {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #64748b;
      letter-spacing: 0.05em;
      padding: 0 0.5rem 0.5rem 0.5rem;
    }
    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.875rem;
      padding: 0.75rem 0.875rem;
      border-radius: 8px;
      color: #94a3b8;
      text-decoration: none;
      transition: all 0.15s ease-in-out;
    }
    .nav-item:hover {
      background-color: #1e293b;
      color: #f8fafc;
    }
    .nav-item.active {
      background: linear-gradient(90deg, #1e3a8a, #1e40af);
      color: #ffffff;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    }
    .nav-icon {
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .nav-label {
      display: flex;
      flex-direction: column;
    }
    .nav-label .primary {
      font-weight: 600;
      font-size: 0.875rem;
    }
    .nav-label .secondary {
      font-size: 0.75rem;
      opacity: 0.8;
    }
    .sidebar-footer {
      padding: 1.25rem;
      border-top: 1px solid #1e293b;
    }
    .cloud-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.75rem;
      color: #94a3b8;
    }
    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: #10b981;
      box-shadow: 0 0 6px #10b981;
    }
  `]
})
export class SidebarComponent {}
