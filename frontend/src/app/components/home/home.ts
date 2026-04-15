import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth-service';
import { ItemDTO } from '../../dtos/itemDTO';
import { UserService } from '../../services/user-service';
import { FavoriteService } from '../../services/favorite-service';
import { Navbar } from '../navbar/navbar';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, FormsModule, Navbar],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit {

  private readonly base = 'https://localhost:7183';

  @ViewChild('categoryStrip') categoryStrip!: ElementRef;

  userName = '';
  isLoading = true;

  allItems: ItemDTO.ItemSummaryDTO[] = [];
  filteredItems: ItemDTO.ItemSummaryDTO[] = [];

  searchQuery = '';
  selectedCategory: string | null = null;
  sortBy = 'newest';
  availableOnly = false;

  showLeftArrow = false;
  showRightArrow = false;

  // Favorites
  favoriteIds = new Set<number>();
  togglingIds = new Set<number>();

  //Toast error emssage
  toastMessage = '';
  toastVisible = false;
  private toastTimeout: any;

  //Pagination
  currentPage = 1;
  pageSize = 20;

  currentUserId = '';


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

  private emojiMap: Record<string, string> = {
    electronics: '📱', tools: '🔧', sports: '⚽', music: '🎸',
    books: '📚', camping: '⛺', photography: '📷', cameras: '📷',
    gaming: '🎮', gardening: '🌱', garden: '🪴', biking: '🚲',
    bikes: '🚲', kitchen: '🍳', cleaning: '🧹', fashion: '👗',
    art: '🎨', baby: '👶', events: '🎉', auto: '🚗',
    other: '📦', others: '📦',
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private userService: UserService,
    private favoriteService: FavoriteService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadUserInfo();
    this.loadItems();
    this.loadFavorites();

    this.route.queryParams.subscribe(params => {
      this.searchQuery = params['q'] || '';
      this.applyFilters();
    });
  }

  ngAfterViewInit(): void {
    const el = this.categoryStrip.nativeElement;

    const updateArrows = () => {
      if (el.scrollLeft > 0) {
        this.showLeftArrow = true;
      
      } else {
        this.showRightArrow = false;
      }

      if (el.scrollLeft + el.clientWidth < el.scrollWidth - 1) {
        this.showRightArrow = true;
      
      } else {
        this.showRightArrow = false;
      }

      this.cdr.detectChanges();
    };

    setTimeout(() => updateArrows(), 100);
    el.addEventListener('scroll', updateArrows);
    window.addEventListener('resize', updateArrows);

    let isMouseDown = false;
    let startMouseX = 0;
    let startScrollLeft = 0;
    let hasDragged =false;

    el.addEventListener('mousedown', (event: MouseEvent) => {
      isMouseDown = true; 
      hasDragged = false;
      startMouseX = event.pageX; 
      startScrollLeft = el.scrollLeft;
      el.style.userSelect = 'none';
    });

    window.addEventListener('mouseup', () => {
      isMouseDown = false;
      el.style.cursor = 'grab';
      el.style.userSelect = '';
    });

    window.addEventListener('mousemove', (e: MouseEvent) => {
      if (!isMouseDown) return;
      
      
      const movedDistance = e.pageX - startMouseX;
      
      if (Math.abs(movedDistance) > 5) {
        hasDragged = true;
        el.style.cursor = 'grabbing';
        el.scrollLeft = startScrollLeft - movedDistance;
      }
    });

    el.addEventListener('click', (e: MouseEvent) => {
      if (hasDragged) { 
        e.stopPropagation(); 
        hasDragged = false; 
      }
    }, true);
  }



  private loadUserInfo(): void {
    this.userService.getMe().subscribe({
      next: (user) => {
        this.userName = user.fullName || user.username;
        this.currentUserId = user.id;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load user info:', err)
    });
  }

  private loadItems(): void {
    this.isLoading = true;
    this.http.get<ItemDTO.ItemSummaryDTO[]>(`${this.base}/api/items`).subscribe({
      next: (items) => {
        this.allItems = items;
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load items:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }


  private loadFavorites(): void {
    if (!this.authService.isLoggedIn()) {
      return;
    };
    
    this.favoriteService.getMyFavorites().subscribe({
      next: (favs) => {
        const ids = favs.map((favs) => favs.item.id);
        this.favoriteIds = new Set(ids);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error("Failed to load favorites: ", error);
       }
    });
  }


  toggleFavorite(itemId: number, event: Event): void {
    event.stopPropagation();
    
    if (this.togglingIds.has(itemId)) {
      return;
    }

    this.togglingIds.add(itemId);

    const isFavorite = this.favoriteIds.has(itemId);
    
    if(isFavorite) {
      this.favoriteService.removeFavorite(itemId).subscribe({
        next: () => {
          this.favoriteIds.delete(itemId);
          this.togglingIds.delete(itemId);
          this.cdr.detectChanges();
        },
        error: (error) => {
          if (error.status === 404) {
            this.favoriteIds.delete(itemId);
            this.showToast('Item not found in favorites.');

          } else {
            
            this.showToast(error.error?.message ?? 'Something went wrong.');
          
          }

          this.togglingIds.delete(itemId);
          this.cdr.detectChanges();

        }
      });
    
    } else {

      this.favoriteService.addFavorite(itemId).subscribe({
        next: () => {
          this.favoriteIds.add(itemId);
          this.togglingIds.delete(itemId);
          this.cdr.detectChanges();
        },
        error: (error) => {
          if (error.status == 409) {
          
            this.favoriteIds.add(itemId);
          this.showToast('Already in your favorites.');
          
          } else {
            this.showToast(error.error?.message ?? 'Something went wrong');
          }  

          this.togglingIds.delete(itemId);
          this.cdr.detectChanges();
        }
        
      });
    }
  }


  onSearch(): void { 
    
    this.applyFilters(); 
  }

  selectCategory(name: string | null): void {
    this.selectedCategory = name;
    this.applyFilters();
  }

  applyFilters(): void {
    this.currentPage = 1;

    let result = [...this.allItems];

    if (this.searchQuery.trim() != '') {
      
      const searchText = this.searchQuery.toLowerCase();

      result = result.filter((item) => {
        return (
          item.title.toLocaleLowerCase().includes(searchText) ||
          item.categoryName.toLowerCase().includes(searchText) || 
          item.pickupAddress.toLowerCase().includes(searchText) ||
          item.ownerName.toLocaleLowerCase().includes(searchText)
        );
      });
    }

    if (this.selectedCategory != null) {
      const selected = this.selectedCategory.toLowerCase();

      result = result.filter((item) => {
        return item.categoryName.toLowerCase() === selected;
      });
    }

    if (this.availableOnly) {
      result = result.filter((item) => {
        return item.isCurrentlyOnLoan === false;
      });
    }

    if (this.sortBy === 'oldest') {
      result.sort((a, b) => a.id - b.id);
    } else if (this.sortBy === 'rating') {
      result.sort((a, b) => b.averageRating - a.averageRating);
    } else if (this.sortBy === 'az') {
      result.sort((a, b) => a.title.localeCompare(b.title));
    } else if (this.sortBy === 'za') {
      result.sort((a, b) => b.title.localeCompare(a.title));
    } else {
      result.sort((a, b) => b.id - a.id);
    }

    this.filteredItems = result;
  }

  scrollCategories(dir: 'left' | 'right'): void {
    
    if (dir === 'right') {
      this.categoryStrip.nativeElement.scrollBy({
        left: 220,
        behavior: 'smooth'
      });
    
    } else {
      
      this.categoryStrip.nativeElement.scrollBy({
        left: -220,
        behavior: 'smooth'
      });
    
    }
  }

  goToItem(id: number): void {

    this.router.navigate(['/items', id]); 
 
  }

  getInitials(name: string): string {
    const parts = name.split(' ');
    const firstLetters = parts.map((part) => part[0]);
    const initials = firstLetters.join('').toUpperCase();

    return initials.slice(0, 2);
  }

  getCategoryEmoji(categoryName: string): string {

    const lowerCategoryName = categoryName.toLowerCase();

    if (this.emojiMap[lowerCategoryName]) {
      return this.emojiMap[lowerCategoryName];
    }
    
    return '📦';
  
  }

  getConditionClass(condition: string): string {
    const lowerCondition = condition?.toLowerCase();

    if (lowerCondition === 'excellent') {
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    }

    if (lowerCondition === 'good') {
      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    }

    if (lowerCondition === 'fair') {
      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
    }

    if (lowerCondition === 'poor') {
      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
    }

    return 'bg-zinc-800 text-zinc-400 border-zinc-700';
  }

  showToast(message: string): void {
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();

    clearTimeout(this.toastTimeout);

    this.toastTimeout = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  get totalPages(): number {
    
    const totalItems = this.filteredItems.length;
    const pages = Math.ceil(totalItems / this.pageSize);
    
    return pages;
  }

  get paginatedItems(): ItemDTO.ItemSummaryDTO[] {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;

    return this.filteredItems.slice(start, end);
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    if (total <= 7) {
      
      for (let i = 1; i <= total; i++) pages.push(i);
    
    } else {
      
      pages.push(1);

      if (current > 3) pages.push(-1); // ellipsis
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push(-1); // ellipsis
      pages.push(total);
    }
    return pages;
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages){
     return;
    }
    this.currentPage = page;
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.cdr.detectChanges();
  }


}