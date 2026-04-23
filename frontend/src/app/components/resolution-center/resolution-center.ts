import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Navbar } from '../navbar/navbar';
import { AuthService } from '../../services/authService';
import { Dispute } from '../dispute/dispute';
import { Appeal } from '../appeal/appeal';
import { VerificationRequest } from '../verification-request/verification-request';
import { Fine } from '../fine/fine';

@Component({
  selector: 'app-resolution-center',
  imports: [CommonModule, Navbar, Dispute, Fine, Appeal, VerificationRequest],
  templateUrl: './resolution-center.html',
  styleUrl: './resolution-center.css',
})
export class ResolutionCenter implements OnInit {

  activeTab: 'disputes' | 'appeals' | 'verification' | 'fines' = 'disputes';

  selectedDisputeId: number | null = null;

  
  tabs = [
    { key: 'disputes'     as const, label: 'Disputes',     icon: '⚖️' },
    { key: 'fines'        as const, label: 'Fines',        icon: '💸' },
    { key: 'appeals'      as const, label: 'Appeals',      icon: '📣' },
    { key: 'verification' as const, label: 'Verification', icon: '🪪' },
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
        this.activeTab = params['tab'] as 'disputes' | 'appeals' | 'fines' | 'verification';
      }

    // pass disputeId down (important part)
    const disputeId = params['disputeId'];
    if (disputeId) {
      this.selectedDisputeId = +disputeId;
    }

    this.cdr.detectChanges();
  });
  }

  setTab(tab: 'disputes' | 'appeals' | 'fines' | 'verification'): void {
    this.activeTab = tab;
    this.router.navigate([], {
      queryParams: { tab },
      queryParamsHandling: 'merge',
    });
  }
}