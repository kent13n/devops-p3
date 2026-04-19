import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthService } from '../auth/auth.service';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  const tokenGetter = vi.fn<() => string | null>();

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { token: tokenGetter } }
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('adds Authorization header on /api requests when token present', () => {
    tokenGetter.mockReturnValue('jwt-abc');

    http.get('/api/files').subscribe();

    const req = httpMock.expectOne('/api/files');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-abc');
    req.flush([]);
  });

  it('does not add Authorization header when no token', () => {
    tokenGetter.mockReturnValue(null);

    http.get('/api/files').subscribe();

    const req = httpMock.expectOne('/api/files');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('does not add Authorization header on non-/api URLs', () => {
    tokenGetter.mockReturnValue('jwt-abc');

    http.get('/assets/file.json').subscribe();

    const req = httpMock.expectOne('/assets/file.json');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
