import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit {
  readonly theme = inject(ThemeService);

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  ngOnInit() {
    // Redirect logged-in users straight to home
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
    }
  }

  steps = [
    { number: '1', icon: '📝', title: 'Opslå' },
    { number: '2', icon: '📅', title: 'Book' },
    { number: '3', icon: '🤝', title: 'Afhent' },
  ];

  categories = [
    { icon: '🔧', name: 'Elværktøj' },
    { icon: '🔨', name: 'Håndværktøj' },
    { icon: '🪴', name: 'Have' },
    { icon: '🚲', name: 'Cykler' },
    { icon: '📷', name: 'Foto' },
    { icon: '⛺', name: 'Camping' },
  ];
}
