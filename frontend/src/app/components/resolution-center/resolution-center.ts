import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { Dispute } from '../dispute/dispute';
import { Appeal } from '../appeal/appeal';
import { VerificationRequest } from '../verification-request/verification-request';
import { Fine } from '../fine/fine';
import { Report } from '../report/report';

@Component({
  selector: 'app-resolution-center',
  imports: [CommonModule, Navbar, Dispute, Fine, Appeal, VerificationRequest, Report],
  templateUrl: './resolution-center.html',
  styleUrl: './resolution-center.css',
})
export class ResolutionCenter implements OnInit {
  activeTab: 'disputes' | 'appeals' | 'verification' | 'fines' | 'reports' = 'disputes';

  selectedDisputeId: number | null = null;
  selectedFineId: number | null = null;
  selectedAppealId: number | null = null;
  selectedVerificationId: number | null = null;
  selectedReportId: number | null = null;

  tabs = [
    { key: 'disputes' as const,     label: 'Tvister',       icon: '⚖️' },
    { key: 'fines' as const,        label: 'Bøder',         icon: '💸' },
    { key: 'appeals' as const,      label: 'Klager',        icon: '📣' },
    { key: 'verification' as const, label: 'Verifikation',  icon: '🪪' },
    { key: 'reports' as const,      label: 'Anmeldelser',   icon: '📊' },
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.subscribe((params) => {
      if (params['tab']) {
        this.activeTab = params['tab'] as
          | 'disputes' | 'appeals' | 'fines' | 'verification' | 'reports';
      }

      if (params['disputeId']) this.selectedDisputeId = +params['disputeId'];
      if (params['fineId']) this.selectedFineId = +params['fineId'];
      if (params['appealId']) this.selectedAppealId = +params['appealId'];
      if (params['verificationId']) this.selectedVerificationId = +params['verificationId'];
      if (params['reportId']) this.selectedReportId = +params['reportId'];

      this.cdr.detectChanges();
    });
  }

  setTab(tab: 'disputes' | 'appeals' | 'fines' | 'verification' | 'reports'): void {
    this.activeTab = tab;
    this.router.navigate([], {
      queryParams: { tab },
      queryParamsHandling: 'merge',
    });
  }
}
