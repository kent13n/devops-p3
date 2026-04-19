import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { FileService } from './file.service';
import { FileDto } from './file.models';

describe('FileService', () => {
  let service: FileService;
  let httpMock: HttpTestingController;

  const fileDto: FileDto = {
    id: 'f-1',
    originalName: 'test.txt',
    sizeBytes: 10,
    mimeType: 'text/plain',
    downloadToken: 'tok-xyz',
    downloadUrl: 'http://host/d/tok-xyz',
    expiresAt: '2026-05-01T00:00:00Z',
    isProtected: false,
    tags: [],
    createdAt: '2026-04-19T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(FileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('upload posts FormData with options to /api/files', () => {
    const file = new File(['hello'], 'hello.txt', { type: 'text/plain' });

    service.upload(file, { expiresInDays: 3, password: 'secret123', tags: ['work', 'urgent'] }).subscribe();

    const req = httpMock.expectOne('/api/files');
    expect(req.request.method).toBe('POST');
    const formData = req.request.body as FormData;
    expect(formData.get('file')).toBeInstanceOf(File);
    expect(formData.get('expiresInDays')).toBe('3');
    expect(formData.get('password')).toBe('secret123');
    expect(formData.get('tags')).toBe('work,urgent');
    req.flush(fileDto);
  });

  it('getMyFiles passes status query param', () => {
    service.getMyFiles('active').subscribe();

    const req = httpMock.expectOne('/api/files?status=active');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('deleteFile issues DELETE to /api/files/:id', () => {
    service.deleteFile('f-1').subscribe();

    const req = httpMock.expectOne('/api/files/f-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('getMetadata fetches /api/download/:token', () => {
    service.getMetadata('tok-xyz').subscribe();

    const req = httpMock.expectOne('/api/download/tok-xyz');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('download sends password payload to /api/download/:token', () => {
    service.download('tok-xyz', 'hunter2').subscribe();

    const req = httpMock.expectOne('/api/download/tok-xyz');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ password: 'hunter2' });
    req.flush(new Blob());
  });

  it('download without password sends empty body', () => {
    service.download('tok-xyz').subscribe();

    const req = httpMock.expectOne('/api/download/tok-xyz');
    expect(req.request.body).toEqual({});
    req.flush(new Blob());
  });
});
