import { CommonModule } from '@angular/common';
<<<<<<< Updated upstream
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
=======
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
>>>>>>> Stashed changes
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-confirm-email',
  imports: [CommonModule, RouterLink],
  templateUrl: './confirm-email.html',
  styleUrl: './confirm-email.css',
})
export class ConfirmEmail implements OnInit {
  status: 'loading' | 'success' | 'error' = 'loading';
  message = '';

  readonly theme = inject(ThemeService);

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    const raw = window.location.search;
    const params = new URLSearchParams(raw);
    const userId = params.get('userId');
    const token = params.get('token');

    if (!userId || !token) {
      this.status = 'error';
      this.message = 'Invalid confirmation link.';
      return;
    }

    this.authService.confirmEmail(userId, token).subscribe({
      next: (res) => {
        this.status = 'success';
        this.message = res.message ?? 'Email confirmed!';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.status = 'error';
        this.message = err.error?.message ?? 'Confirmation failed. The link may have expired.';
        this.cdr.detectChanges();
      },
    });
  }
}
