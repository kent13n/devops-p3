import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FileDto, FileMetadataDto, TagDto } from './file.models';

@Injectable({ providedIn: 'root' })
export class FileService {
  constructor(private http: HttpClient) {}

  upload(file: File, options?: {
    expiresInDays?: number;
    password?: string;
    tags?: string[];
  }): Observable<FileDto> {
    return this.http.post<FileDto>('/api/files', this.buildFormData(file, options));
  }

  uploadWithProgress(file: File, options?: {
    expiresInDays?: number;
    password?: string;
    tags?: string[];
  }): Observable<HttpEvent<FileDto>> {
    const req = new HttpRequest<FormData>('POST', '/api/files', this.buildFormData(file, options), {
      reportProgress: true
    });
    return this.http.request<FileDto>(req);
  }

  deleteFile(id: string): Observable<void> {
    return this.http.delete<void>(`/api/files/${id}`);
  }

  getMetadata(token: string): Observable<FileMetadataDto> {
    return this.http.get<FileMetadataDto>(`/api/download/${token}`);
  }

  download(token: string, password?: string): Observable<Blob> {
    return this.http.post(`/api/download/${token}`,
      password ? { password } : {},
      { responseType: 'blob' }
    );
  }

  getTags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>('/api/tags');
  }

  deleteTag(id: string): Observable<void> {
    return this.http.delete<void>(`/api/tags/${id}`);
  }

  private buildFormData(file: File, options?: {
    expiresInDays?: number;
    password?: string;
    tags?: string[];
  }): FormData {
    const formData = new FormData();
    formData.append('file', file);

    if (options?.expiresInDays)
      formData.append('expiresInDays', options.expiresInDays.toString());

    if (options?.password)
      formData.append('password', options.password);

    if (options?.tags?.length)
      formData.append('tags', options.tags.join(','));

    return formData;
  }
}
