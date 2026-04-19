import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let routerNav: ReturnType<typeof vi.fn>;

  function runGuard() {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
  }

  beforeEach(() => {
    routerNav = vi.fn();
  });

  it('returns true when authenticated', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: Router, useValue: { navigate: routerNav } }
      ]
    });

    const result = runGuard();

    expect(result).toBe(true);
    expect(routerNav).not.toHaveBeenCalled();
  });

  it('returns false and navigates to / when not authenticated', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => false } },
        { provide: Router, useValue: { navigate: routerNav } }
      ]
    });

    const result = runGuard();

    expect(result).toBe(false);
    expect(routerNav).toHaveBeenCalledWith(['/']);
  });
});
