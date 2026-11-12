import { Injectable } from '@angular/core';

export interface StoredChunkedSession {
  uploadId: string;
  blobName: string;
  fileName: string;
  fileSize: number;
  chunkSize: number;
  totalChunks: number;
  blockIds: string[];
  sasUrl: string;
  expiresAt: string;
  lastUpdated: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChunkedStorageService {
  private readonly STORAGE_PREFIX = 'azure_upload_session_';

  saveSession(session: StoredChunkedSession): void {
    try {
      const key = this.getKey(session.fileName, session.fileSize);
      localStorage.setItem(key, JSON.stringify(session));
    } catch (e) {
      console.warn('Could not save upload session to localStorage', e);
    }
  }

  getSession(fileName: string, fileSize: number): StoredChunkedSession | null {
    try {
      const key = this.getKey(fileName, fileSize);
      const data = localStorage.getItem(key);
      if (!data) return null;

      const session: StoredChunkedSession = JSON.parse(data);
      // Check if session has expired
      if (new Date(session.expiresAt).getTime() < Date.now()) {
        this.clearSession(fileName, fileSize);
        return null;
      }
      return session;
    } catch {
      return null;
    }
  }

  clearSession(fileName: string, fileSize: number): void {
    try {
      const key = this.getKey(fileName, fileSize);
      localStorage.removeItem(key);
    } catch (e) {
      console.warn('Could not clear upload session', e);
    }
  }

  private getKey(fileName: string, fileSize: number): string {
    return `${this.STORAGE_PREFIX}${fileName}_${fileSize}`;
  }
}
