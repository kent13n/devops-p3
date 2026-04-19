import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { AuthResponse } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const authResponse: AuthResponse = {
    user: { id: 'u-1', email: 'test@local', createdAt: '2026-01-01T00:00:00Z' },
    token: 'jwt-abc',
    expiresAt: '2026-01-02T00:00:00Z'
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('login stores token/user and updates signals', () => {
    service.login({ email: 'test@local', password: 'Password123!' }).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(authResponse);

    expect(service.token()).toBe('jwt-abc');
    expect(service.currentUser()?.email).toBe('test@local');
    expect(service.isAuthenticated()).toBe(true);
    expect(localStorage.getItem('datashare_token')).toBe('jwt-abc');
  });

  it('register stores token/user and updates signals', () => {
    service.register({ email: 'new@local', password: 'Password123!' }).subscribe();

    const req = httpMock.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    req.flush(authResponse);

    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout clears storage and signals', () => {
    service.login({ email: 'test@local', password: 'Password123!' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(authResponse);

    service.logout();

    expect(service.token()).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('datashare_token')).toBeNull();
  });

  it('loadFromStorage restores session from localStorage', () => {
    localStorage.setItem('datashare_token', 'stored-jwt');
    localStorage.setItem('datashare_user', JSON.stringify(authResponse.user));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const reloaded = TestBed.inject(AuthService);

    expect(reloaded.token()).toBe('stored-jwt');
    expect(reloaded.currentUser()?.email).toBe('test@local');
    expect(reloaded.isAuthenticated()).toBe(true);
  });

  it('loadFromStorage logs out if user JSON is corrupted', () => {
    localStorage.setItem('datashare_token', 'stored-jwt');
    localStorage.setItem('datashare_user', '{not-json');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const reloaded = TestBed.inject(AuthService);

    expect(reloaded.isAuthenticated()).toBe(false);
  });
});
