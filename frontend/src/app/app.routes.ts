import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { AuthService } from './services/authService';
import { UserProfile } from './components/user/user';
import { Landing } from './components/landing/landing';
import { Home } from './components/home/home';
import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { ConfirmEmail } from './components/confirm-email/confirm-email';
import { ItemDetail } from './components/item-detail/item-detail';
import { PageNotFound } from './components/page-not-found/page-not-found';
import { UserDashboard } from './components/user-dashboard/user-dashboard';


export const authGuard : CanActivateFn = () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    return auth.isLoggedIn() ? true : router.createUrlTree(['/']);
}

export const routes: Routes = [
    {path: '', component: Landing},
    {path: 'login', component: Login},
    {path: 'register', component: Register},
    {path: 'auth/confirm', component: ConfirmEmail},
    {path: 'home', component: Home, canActivate: [authGuard]},
    {path: 'items/:slug', component: ItemDetail, canActivate: [authGuard]},
    {path: 'users/:id', component: UserProfile, canActivate: [authGuard]},
    {path: 'my-dashboard', component: UserDashboard, canActivate: [authGuard]},


    { path: '404', component: PageNotFound },
    { path: '**', redirectTo: '/404' }

];
