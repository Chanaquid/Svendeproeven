import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
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
      this.message = 'Ugyldigt bekræftelseslink.';
      return;
    }

    this.authService.confirmEmail(userId, token).subscribe({
      next: (res) => {
        this.status = 'success';
        this.message = res.message ?? 'E-mail bekræftet!';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.status = 'error';
        this.message = err.error?.message ?? 'Bekræftelse mislykkedes. Linket er muligvis udløbet.';
        this.cdr.detectChanges();
      },
    });
  }
}
