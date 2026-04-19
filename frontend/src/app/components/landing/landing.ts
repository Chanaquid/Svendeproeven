import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/authService';
import { ItemService } from '../../services/itemService';
import { UserService } from '../../services/userService';
import { ItemAvailability } from '../../dtos/enums';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterLink, Navbar],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit{

  featuredItems: any[] = [];
  stats = {
    activeItems: '...',
    members: '...',
    cities: '1000+',
  };


   constructor(private authService: AuthService,
     private router: Router,
      private itemService: ItemService,
       private userService : UserService,
      private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    //Redirect logged-in users straight to the home/dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
    }

    this.loadStats();
  }

  private loadStats() {

    this.itemService.getAvailableCount().subscribe({
      next: (res) => {
        this.stats.activeItems = res.data + '+';
        console.log('Available items count:', res.data);
        this.cdr.detectChanges(); 

      },
      error : (err) => {
        this.stats.activeItems = '250+';
        this.cdr.detectChanges();
      }
  });

    this.itemService.getLatest(4).subscribe({
      next: (res) => {
        const items = res.data ?? []; 
        console.log(items);
        console.log(this.stats.members)

        this.featuredItems = items.map(item => ({
          id: item.id,
          emoji: this.getCategoryEmoji(item.categoryName),
          category: item.categoryName,
          title: item.title,
          location: item.pickupAddress,
          owner: item.ownerName,
          ownerInitials: item.ownerName.split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2),
          rating: item.averageRating && item.averageRating > 0 ? `${item.averageRating.toFixed(1)} (${item.totalReviews})`
          : 'New', 
          available: item.availability === ItemAvailability.Available,          
          primaryPhotoUrl: item.mainPhotoUrl,
        }));
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
      error: (err) => {
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      }

    });

    this.userService.getTotalUsersCount().subscribe({
      next: (users) => {
        
        this.stats.members = users.data + '+';
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW

      },
      error: (err) => {
        
        this.stats.members = '5000+'
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW

      }
    });
  }

  private getCategoryEmoji(categoryName: string): string {
    const map: Record<string, string> = {
      tools: '🔧', photography: '📷', bikes: '🚲', outdoors: '🏕️',
      music: '🎸', gaming: '🎮', books: '📚', travel: '🧳',
      garden: '🪴', art: '🎨', fashion: '👗', kitchen: '🍳',
      electronics: '📱', sports: '⚽', camping: '🏕️',
       others: '📦'

    };
    return map[categoryName.toLowerCase()] ?? '📦';
  }



  steps = [
    {
      number: '01',
      icon: '🔍',
      title: 'Find what you need',
      desc: 'Browse thousands of items listed by people near you. Filter by category, condition, or availability.',
    },
    {
      number: '02',
      icon: '📅',
      title: 'Request a loan',
      desc: 'Pick your dates and send a request. The owner reviews and approves within hours.',
    },
    {
      number: '03',
      icon: '🤝',
      title: 'Pick up & return',
      desc: 'Collect the item, use it, and return it on time. Your trust score grows with every smooth loan.',
    },
    {
      number: '04',
      icon: '⭐',
      title: 'Rate & repeat',
      desc: 'Leave a review and build your reputation. Higher scores unlock more items instantly.',
    },
  ];

  categories = [
    { icon: '📱', name: 'Electronics' },
    { icon: '🔧', name: 'Tools' },
    { icon: '⚽', name: 'Sports' },
    { icon: '🎸', name: 'Music' },
    { icon: '📚', name: 'Books' },
    { icon: '⛺', name: 'Camping' },
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
      desc: 'Item condition is documented before every loan so there\'s hardly ever dispute about damage.',
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




}
