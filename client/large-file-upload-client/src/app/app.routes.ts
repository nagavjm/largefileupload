import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'direct-upload',
    pathMatch: 'full'
  },
  {
    path: 'direct-upload',
    loadComponent: () =>
      import('./features/direct-upload/components/direct-upload.component').then(
        m => m.DirectUploadComponent
      )
  },
  {
    path: 'chunked-upload',
    loadComponent: () =>
      import('./features/chunked-upload/components/chunked-upload.component').then(
        m => m.ChunkedUploadComponent
      ).catch(() =>
        import('./features/direct-upload/components/direct-upload.component').then(
          m => m.DirectUploadComponent
        )
      )
  },
  {
    path: '**',
    redirectTo: 'direct-upload'
  }
];
