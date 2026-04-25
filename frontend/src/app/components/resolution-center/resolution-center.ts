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
    { key: 'disputes'     as const, label: 'Disputes',     icon: '⚖️' },
    { key: 'fines'        as const, label: 'Fines',        icon: '💸' },
    { key: 'appeals'      as const, label: 'Appeals',      icon: '📣' },
    { key: 'verification' as const, label: 'Verification', icon: '🪪' },
    { key: 'reports'      as const, label: 'Reports',      icon: '📊' },
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

     this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        this.activeTab = params['tab'] as 'disputes' | 'appeals' | 'fines' | 'verification' | 'reports';
      }

      // pass disputeId down (important part)
      const disputeId = params['disputeId'];
      if (disputeId) {
        this.selectedDisputeId = +disputeId;
      }

      const fineId = params['fineId'];
      if (fineId) {
        this.selectedFineId = +fineId;
      }

        const appealId = params['appealId'];
      if (appealId) {
        this.selectedAppealId = +appealId;
      }

      const verificationId = params['verificationId'];
      if (verificationId) {
        this.selectedVerificationId = +verificationId;
      }
      
      const reportId = params['reportId'];
      if (reportId) {
        this.selectedReportId = +reportId;
      }



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