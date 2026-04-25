import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';

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

  constructor(private authService: AuthService, private router: Router, private cdr: ChangeDetectorRef) {}

  login() {
    this.isLoading = true;
  this.errorMessage = '';
  const dto = { email: this.email, password: this.password };

  this.authService.login(dto).subscribe({
    next: (res) => {
      this.isLoading = false;
      console.log('Login successful:', res);
      this.router.navigate(['/home']);
    },
    error: (err) => {
      this.isLoading = false;
      const msg: string = err.error?.message ?? '';

      if (msg.startsWith('TEMP_BAN|')) {
        const [, utcDate, reason] = msg.split('|');
        const local = new Date(utcDate).toLocaleString(undefined, {
          dateStyle: 'medium',
          timeStyle: 'short'
        });
        this.errorMessage = `Your account is temporarily banned until ${local}. Reason: ${reason}`;
      } else if (msg.startsWith('PERM_BAN|')) {
        const reason = msg.replace('PERM_BAN|', '');
        this.errorMessage = `Your account has been permanently banned. Reason: ${reason}`;
      } else {
        this.errorMessage = msg || 'Invalid email or password';
      }

      this.cdr.detectChanges();
    }
  });
  }

  togglePassword() {
      this.showPassword = !this.showPassword;
  }


}
