import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError(error => {
      // 401 sur /api/auth/* = identifiants invalides (géré dans le composant)
      // 401 sur /api/download/* = mot de passe fichier incorrect (géré dans le composant)
      // 401 ailleurs = JWT expiré → logout
      const isAuthEndpoint = req.url.includes('/api/auth/');
      const isDownloadEndpoint = req.url.includes('/api/download/');
      if (error.status === 401 && !isAuthEndpoint && !isDownloadEndpoint) {
        authService.logout();
        router.navigate(['/']);
      }
      return throwError(() => error);
    })
  );
};
