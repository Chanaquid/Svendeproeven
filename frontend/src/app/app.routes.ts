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
import { TestUpload } from './components/test-upload/test-upload';
import { Item } from './components/item/item';
import { Loan } from './components/loan/loan';
import { Favorite } from './components/favorite/favorite';
import { LoanDetail } from './components/loan-detail/loan-detail';
import { ResolutionCenter } from './components/resolution-center/resolution-center';
import { BlockedUser } from './components/blocked-user/blocked-user';
import { Notification } from './components/notification/notification';
import { DirectChat } from './components/direct-chat/direct-chat';
import { AdminUser } from './components/admin-user/admin-user';
import { AdminGuardService } from './services/adminGuardService';
import { AdminDashboard } from './components/admin-dashboard/admin-dashboard';
import { AdminItem } from './components/admin-item/admin-item';
import { AdminLoan } from './components/admin-loan/admin-loan';
import { AdminVerification } from './components/admin-verification/admin-verification';
import { AdminDispute } from './components/admin-dispute/admin-dispute';
import { AdminFine } from './components/admin-fine/admin-fine';
import { AdminAppeal } from './components/admin-appeal/admin-appeal';
import { AdminReport } from './components/admin-report/admin-report';
import { SupportChat } from './components/support-chat/support-chat';
import { Map } from './components/map/map';


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
    {path: 'blocked-user', component: BlockedUser, canActivate: [authGuard]},
    {path: 'my-items', component: Item, canActivate: [authGuard]},
    {path: 'my-chats', component: DirectChat, canActivate: [authGuard]},
    {path: 'my-loans', component: Loan, canActivate: [authGuard]},
    {path: 'loans/:id', component: LoanDetail, canActivate: [authGuard]},
    {path: 'resolution-center', component: ResolutionCenter, canActivate: [authGuard]},
    {path: 'notifications', component: Notification, canActivate: [authGuard]},
    {path: 'my-favorites', component: Favorite, canActivate: [authGuard]},
    { path: 'supports', component: SupportChat, canActivate: [authGuard] },
    { path: 'maps', component: Map, canActivate: [authGuard] },


    { path: 'admin-dashboard', component: AdminDashboard, canActivate: [AdminGuardService] },
     { path: 'admin-users', component: AdminUser, canActivate: [AdminGuardService] },
     { path: 'admin-items', component: AdminItem, canActivate: [AdminGuardService] },
     { path: 'admin-loans', component: AdminLoan, canActivate: [AdminGuardService] },
     { path: 'admin-verifications', component: AdminVerification, canActivate: [AdminGuardService] },
     { path: 'admin-disputes', component: AdminDispute, canActivate: [AdminGuardService] },
     { path: 'admin-fines', component: AdminFine, canActivate: [AdminGuardService] },
     { path: 'admin-appeals', component: AdminAppeal, canActivate: [AdminGuardService] },
     { path: 'admin-reports', component: AdminReport, canActivate: [AdminGuardService] },


    {path: 'test', component: TestUpload},


    { path: '404', component: PageNotFound },
    { path: '**', redirectTo: '/404' }

];
