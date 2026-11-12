import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent],
  template: `
    <div class="app-shell">
      <app-sidebar></app-sidebar>
      <div class="main-viewport">
        <header class="topbar">
          <div class="topbar-left">
            <span class="env-tag">Dev & Production Ready</span>
          </div>
          <div class="topbar-right">
            <div class="user-profile">
              <span class="user-avatar">JD</span>
              <span class="user-name">dev-user-001</span>
            </div>
          </div>
        </header>

        <main class="content-area">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .app-shell {
      display: flex;
      width: 100vw;
      height: 100vh;
      overflow: hidden;
      background-color: #f8fafc;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    }
    .main-viewport {
      flex: 1;
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
    }
    .topbar {
      height: 64px;
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 2rem;
      flex-shrink: 0;
    }
    .env-tag {
      background-color: #f1f5f9;
      color: #475569;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.25rem 0.625rem;
      border-radius: 9999px;
      border: 1px solid #e2e8f0;
    }
    .topbar-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .user-profile {
      display: flex;
      align-items: center;
      gap: 0.625rem;
    }
    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background-color: #3b82f6;
      color: white;
      font-size: 0.75rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .user-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: #334155;
    }
    .content-area {
      flex: 1;
      overflow-y: auto;
      padding: 2rem;
    }
  `]
})
export class MainLayoutComponent {}
