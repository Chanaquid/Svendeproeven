import { CommonModule } from '@angular/common';
<<<<<<< Updated upstream
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
=======
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { UserService } from '../../services/userService';
import { ItemAvailability } from '../../dtos/enums';
>>>>>>> Stashed changes
import { ThemeService } from '../../services/themeService';

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit {
<<<<<<< Updated upstream
=======
  featuredItems: any[] = [];
  stats = {
    activeItems: '...',
    members: '...',
    cities: '1000+',
  };
>>>>>>> Stashed changes
  readonly theme = inject(ThemeService);

  constructor(
    private authService: AuthService,
    private router: Router,
<<<<<<< Updated upstream
=======
    private itemService: ItemService,
    private userService: UserService,
    private cdr: ChangeDetectorRef,
>>>>>>> Stashed changes
  ) {}

  ngOnInit() {
    // Redirect logged-in users straight to home
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
    }
  }

<<<<<<< Updated upstream
=======
  private loadStats() {
    this.itemService.getAvailableCount().subscribe({
      next: (res) => {
        this.stats.activeItems = res.data + '+';
        console.log('Available items count:', res.data);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.stats.activeItems = '250+';
        this.cdr.detectChanges();
      },
    });

    this.itemService.getLatest(4).subscribe({
      next: (res) => {
        const items = res.data ?? [];
        console.log(items);
        console.log(this.stats.members);

        this.featuredItems = items.map((item) => ({
          id: item.id,
          emoji: this.getCategoryEmoji(item.categoryName),
          category: item.categoryName,
          title: item.title,
          location: item.pickupAddress,
          owner: item.ownerName,
          ownerInitials: item.ownerName
            .split(' ')
            .map((n: string) => n[0])
            .join('')
            .toUpperCase()
            .slice(0, 2),
          rating:
            item.averageRating && item.averageRating > 0
              ? `${item.averageRating.toFixed(1)} (${item.totalReviews})`
              : 'New',
          available: item.availability === ItemAvailability.Available,
          primaryPhotoUrl: item.mainPhotoUrl,
        }));
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
      error: (err) => {
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
    });

    this.userService.getTotalUsersCount().subscribe({
      next: (users) => {
        this.stats.members = users.data + '+';
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
      error: (err) => {
        this.stats.members = '5000+';
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
    });
  }

  private getCategoryEmoji(categoryName: string): string {
    const map: Record<string, string> = {
      tools: '🔧',
      photography: '📷',
      bikes: '🚲',
      outdoors: '🏕️',
      music: '🎸',
      gaming: '🎮',
      books: '📚',
      travel: '🧳',
      garden: '🪴',
      art: '🎨',
      fashion: '👗',
      kitchen: '🍳',
      electronics: '📱',
      sports: '⚽',
      camping: '🏕️',
      others: '📦',
    };
    return map[categoryName.toLowerCase()] ?? '📦';
  }

>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
  ];
=======
    { icon: '📷', name: 'Photography' },
    { icon: '🎮', name: 'Gaming' },
    { icon: '🌱', name: 'Gardening' },
    { icon: '🚲', name: 'Biking' },
    { icon: '🍳', name: 'Kitchen' },
    { icon: '🧹', name: 'Cleaning' },
    { icon: '👗', name: 'Fashion' },
    { icon: '🎨', name: 'Art' },
    { icon: '👶', name: 'Baby' },
    { icon: '🎉', name: 'Events' },
    { icon: '🚗', name: 'Auto' },
    { icon: '📦', name: 'Other' },
  ];

  trustItems = [
    {
      icon: '🏅',
      title: 'Trust Score',
      desc: 'Every member earns a live score based on returns, reviews, and behaviour.',
    },
    {
      icon: '📸',
      title: 'Photo Snapshots',
      desc: "Item condition is documented before every loan so there's hardly ever dispute about damage.",
    },
    {
      icon: '⚖️',
      title: 'Dispute Resolution',
      desc: 'Our admin team reviews evidence from both sides and issues fair verdicts within 72h.',
    },
    {
      icon: '✅',
      title: 'ID Verification',
      desc: 'Verified members unlock higher-value items and get a badge on their profile.',
    },
  ];
>>>>>>> Stashed changes
}
