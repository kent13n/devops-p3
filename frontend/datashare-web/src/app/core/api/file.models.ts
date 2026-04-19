export interface FileDto {
  id: string;
  originalName: string;
  sizeBytes: number;
  mimeType: string;
  downloadToken: string;
  downloadUrl: string;
  expiresAt: string;
  isProtected: boolean;
  tags: string[];
  createdAt: string;
}

export interface FileMetadataDto {
  originalName: string;
  sizeBytes: number;
  mimeType: string;
  expiresAt: string;
  isProtected: boolean;
}

export interface DownloadRequest {
  password?: string;
}

export interface TagDto {
  id: string;
  name: string;
}
