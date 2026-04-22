import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './authService';

export const authInterceptorService: HttpInterceptorFn = (req, next) => {

   if (req.url.startsWith('https://api.geoapify.com') ||
      req.url.startsWith('https://nominatim.openstreetmap.org')) {
    return next(req);
  }

  const token = inject(AuthService).getToken();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
}; 