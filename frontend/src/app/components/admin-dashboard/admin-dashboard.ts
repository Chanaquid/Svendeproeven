import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Navbar } from '../navbar/navbar';
import { AdminDashboardDto } from '../../dtos/adminDto';
import { AuthService } from '../../services/authService';
import { AdminService } from '../../services/adminService';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule, RouterLink, Navbar],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard implements OnInit {

  dashboard: AdminDashboardDto | null = null;
  isLoading = true;
  error: string | null = null;

  constructor(
    private authService: AuthService,
    private adminService: AdminService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn() || !this.authService.isAdmin()) {
      this.router.navigate(['/home']);
      return;
    }
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.error = null;
    this.adminService.getDashboard().subscribe({
      next: (data: AdminDashboardDto) => {
        this.dashboard = data;
        console.log(this.dashboard);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load dashboard.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get allClear(): boolean {
    if (!this.dashboard) return false;
    return (
      this.dashboard.pendingItemApprovals === 0 &&
      this.dashboard.pendingLoanApprovals === 0 &&
      this.dashboard.openDisputes === 0 &&
      this.dashboard.overdueDisputeResponses === 0 &&
      this.dashboard.pendingAppeals === 0 &&
      this.dashboard.pendingUserVerifications === 0 &&
      this.dashboard.pendingFines === 0 &&
      this.dashboard.pendingReports === 0 &&
      this.dashboard.pendingSupports === 0
    );
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }
}