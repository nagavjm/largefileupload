import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DirectUploadInitiationResponse,
  CompleteDirectUploadResponse,
  UploadSessionDto,
  PaginatedList,
  ChunkedUploadInitiationResponse,
  ChunkedUploadBlocksResponse,
  ChunkedUploadCommitResponse
} from '../models/upload.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  // Direct Upload APIs
  initiateDirectUpload(
    fileName: string,
    fileSize: number,
    contentType: string
  ): Observable<DirectUploadInitiationResponse> {
    return this.http.post<DirectUploadInitiationResponse>(
      `${this.baseUrl}/uploads/direct/initiate`,
      { fileName, fileSize, contentType }
    );
  }

  completeDirectUpload(uploadId: string): Observable<CompleteDirectUploadResponse> {
    return this.http.post<CompleteDirectUploadResponse>(
      `${this.baseUrl}/uploads/direct/${uploadId}/complete`,
      {}
    );
  }

  // Chunked Upload APIs
  initiateChunkedUpload(
    fileName: string,
    fileSize: number,
    contentType: string,
    chunkSize: number = 4194304
  ): Observable<ChunkedUploadInitiationResponse> {
    return this.http.post<ChunkedUploadInitiationResponse>(
      `${this.baseUrl}/uploads/chunked/initiate`,
      { fileName, fileSize, contentType, chunkSize }
    );
  }

  getChunkedUploadBlocks(uploadId: string): Observable<ChunkedUploadBlocksResponse> {
    return this.http.get<ChunkedUploadBlocksResponse>(
      `${this.baseUrl}/uploads/chunked/${uploadId}/blocks`
    );
  }

  commitChunkedUpload(uploadId: string, blockIds: string[]): Observable<ChunkedUploadCommitResponse> {
    return this.http.post<ChunkedUploadCommitResponse>(
      `${this.baseUrl}/uploads/chunked/${uploadId}/commit`,
      { blockIds }
    );
  }

  cancelChunkedUpload(uploadId: string, reason?: string): Observable<{ uploadId: string; status: string }> {
    return this.http.post<{ uploadId: string; status: string }>(
      `${this.baseUrl}/uploads/chunked/${uploadId}/cancel`,
      { reason }
    );
  }

  // Common Upload APIs
  getUploadSession(uploadId: string): Observable<UploadSessionDto> {
    return this.http.get<UploadSessionDto>(`${this.baseUrl}/uploads/${uploadId}`);
  }

  getUploadHistory(page: number = 1, pageSize: number = 20): Observable<PaginatedList<UploadSessionDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PaginatedList<UploadSessionDto>>(`${this.baseUrl}/uploads`, { params });
  }

  deleteUploadSession(uploadId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/uploads/${uploadId}`);
  }
}
