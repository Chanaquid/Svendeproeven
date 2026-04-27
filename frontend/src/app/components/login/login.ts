import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email = '';
  password = '';
  errorMessage = '';
  isLoading = false;
  showPassword = false;

  readonly theme = inject(ThemeService);

  constructor(
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  login() {
    this.isLoading = true;
    this.errorMessage = '';
    const dto = { email: this.email, password: this.password };

    this.authService.login(dto).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.isLoading = false;
        const msg: string = err.error?.message ?? '';

        if (msg.startsWith('TEMP_BAN|')) {
          const [, utcDate, reason] = msg.split('|');
          const local = new Date(utcDate).toLocaleString('da-DK', {
            dateStyle: 'medium',
            timeStyle: 'short',
          });
          this.errorMessage = `Din konto er midlertidigt blokeret indtil ${local}. Årsag: ${reason}`;
        } else if (msg.startsWith('PERM_BAN|')) {
          const reason = msg.replace('PERM_BAN|', '');
          this.errorMessage = `Din konto er permanent blokeret. Årsag: ${reason}`;
        } else {
          this.errorMessage = msg || 'Forkert e-mail eller adgangskode';
        }
        this.cdr.detectChanges();
      },
    });
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }
}
