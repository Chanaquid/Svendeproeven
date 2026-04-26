import {
  AfterViewInit, ChangeDetectorRef, Component,
  ElementRef, OnDestroy, OnInit, ViewChild, ViewEncapsulation
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as L from 'leaflet';
import { Subject, debounceTime } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Router } from '@angular/router';
import { Navbar } from '../navbar/navbar';
import { ItemService } from '../../services/itemService';
import { ItemListDto } from '../../dtos/itemDto';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { ItemFilter } from '../../dtos/filterDto';
import { getPageNumbers, getTotalPages } from '../../utils/pagination.utils';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';

const GEOAPIFY_KEY = '6efe16ed3bb047b8975d6f4738a471a9';

@Component({
  selector: 'app-map',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './map.html',
  styleUrl: './map.css',
  encapsulation: ViewEncapsulation.None,
})
export class Map implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('categoryStrip') categoryStrip!: ElementRef;

  // ── Map state ─────────────────────────────────────────────────────────────
  private map!: L.Map;
  private centroid: L.LatLngExpression = [55.6761, 12.5683];
  private markers: L.Marker[] = [];
  private radiusCircle: L.Circle | null = null;
  private userMarker: L.Marker | null = null;
  private destroy$ = new Subject<void>();

  // ── Items & UI ────────────────────────────────────────────────────────────
  items: ItemListDto[] = [];
  selectedItem: ItemListDto | null = null;
  isLoading = false;
  isInitialLoad = true;
  locationQuery = '';
  isGeocodingLoading = false;

  // ── Location autocomplete ─────────────────────────────────────────────────
  locationSuggestions: any[] = [];
  showSuggestions = false;
  private suggestionTimeout: any;

  // ── Filters ───────────────────────────────────────────────────────────────
  radiusKm = 5;
  searchQuery = '';
  selectedCategory: string | null = null;
  sortLabel = 'newest';
  sortBy = 'createdAt';
  sortDescending = true;
  availableOnly = false;
  freeOnly = false;
  showLeftArrow = false;
  showRightArrow = false;

  readonly radiusOptions = [1, 2, 5, 10, 20, 50];

  // ── Pagination ────────────────────────────────────────────────────────────
  currentPage = 1;
  totalCount = 0;
  readonly pageSize = 12;

  // ── RxJS ──────────────────────────────────────────────────────────────────
  private searchSubject = new Subject<string>();

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

  private categoryIdMap: Record<string, number> = {
    'Electronics': 1, 'Tools': 2, 'Sports': 3, 'Music': 4,
    'Books': 5, 'Camping': 6, 'Photography': 7, 'Gaming': 8,
    'Gardening': 9, 'Biking': 10, 'Kitchen': 11, 'Cleaning': 12,
    'Fashion': 13, 'Art': 14, 'Baby': 15, 'Events': 16,
    'Auto': 17, 'Other': 18,
  };

  constructor(
    private itemService: ItemService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.searchSubject.pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => { this.currentPage = 1; this.loadNearbyItems(); });

    this.getUserLocation();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      if (this.map) this.map.invalidateSize();
      this.initResizeHandle();
      this.setupCategoryArrows();
    }, 300);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    clearTimeout(this.suggestionTimeout);
  }

  // ── Resize handle ─────────────────────────────────────────────────────────

  private initResizeHandle(): void {
    const handle = document.getElementById('resizeHandle');
    const aside  = document.getElementById('mapAside');
    if (!handle || !aside) return;

    let isDragging = false;
    let startX     = 0;
    let startWidth = 0;

    handle.addEventListener('mousedown', (e: MouseEvent) => {
      isDragging = true;
      startX     = e.clientX;
      startWidth = aside.offsetWidth;
      handle.classList.add('dragging');
      document.body.style.cursor     = 'col-resize';
      document.body.style.userSelect = 'none';
      e.preventDefault();
    });

    document.addEventListener('mousemove', (e: MouseEvent) => {
      if (!isDragging) return;
      const delta    = e.clientX - startX;
      const newWidth = Math.min(Math.max(startWidth + delta, 260), window.innerWidth * 0.7);
      aside.style.width = newWidth + 'px';
      if (this.map) this.map.invalidateSize();
    });

    document.addEventListener('mouseup', () => {
      if (!isDragging) return;
      isDragging = false;
      handle.classList.remove('dragging');
      document.body.style.cursor     = '';
      document.body.style.userSelect = '';
      if (this.map) this.map.invalidateSize();
    });
  }

  // ── Location ──────────────────────────────────────────────────────────────

  private getUserLocation(): void {
    if (!navigator.geolocation) { this.initMap(); return; }
    navigator.geolocation.getCurrentPosition(
      (pos) => { this.centroid = [pos.coords.latitude, pos.coords.longitude]; this.initMap(); },
      ()    => { this.centroid = [55.6761, 12.5683]; this.initMap(); }
    );
  }

  onLocationInput(): void {
    const q = this.locationQuery.trim();
    this.showSuggestions = false;
    clearTimeout(this.suggestionTimeout);

    if (q.length < 3) {
      this.locationSuggestions = [];
      return;
    }

    this.suggestionTimeout = setTimeout(() => {
      this.isGeocodingLoading = true;
      this.cdr.detectChanges();

      const url = `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(q)}&limit=5&apiKey=${GEOAPIFY_KEY}`;

      fetch(url)
        .then(r => r.json())
        .then(data => {
          this.locationSuggestions = data.features ?? [];
          this.showSuggestions     = this.locationSuggestions.length > 0;
          this.isGeocodingLoading  = false;
          this.cdr.detectChanges();
        })
        .catch(() => {
          this.isGeocodingLoading = false;
          this.cdr.detectChanges();
        });
    }, 400);
  }

  onLocationKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      this.showSuggestions = false;
      return;
    }
    if (e.key === 'Enter' && this.locationQuery.trim().length > 2) {
      this.showSuggestions = false;
      this.geocodeLocationDirect(this.locationQuery);
    }
  }

  onLocationBlur(): void {
    // Delay so mousedown on a suggestion fires before the list closes
    setTimeout(() => { this.showSuggestions = false; this.cdr.detectChanges(); }, 200);
  }

  selectLocationSuggestion(place: any): void {
    const props = place.properties;
    this.locationQuery       = props.formatted;
    this.showSuggestions     = false;
    this.locationSuggestions = [];
    this.centroid = [place.geometry.coordinates[1], place.geometry.coordinates[0]];
    this.updateCenter();
    this.cdr.detectChanges();
  }

  // Fallback: geocode raw text using Geoapify when user presses Enter
  private async geocodeLocationDirect(query: string): Promise<void> {
    this.isGeocodingLoading = true;
    this.cdr.detectChanges();
    try {
      const url  = `https://api.geoapify.com/v1/geocode/search?text=${encodeURIComponent(query)}&limit=1&apiKey=${GEOAPIFY_KEY}`;
      const res  = await fetch(url);
      const data = await res.json();
      const feat = data.features?.[0];
      if (feat) {
        this.centroid      = [feat.geometry.coordinates[1], feat.geometry.coordinates[0]];
        this.locationQuery = feat.properties.formatted;
        this.updateCenter();
      }
    } catch { /* silent */ }
    this.isGeocodingLoading = false;
    this.cdr.detectChanges();
  }

  useMyLocation(): void {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition((pos) => {
      this.centroid          = [pos.coords.latitude, pos.coords.longitude];
      this.locationQuery     = '';
      this.showSuggestions   = false;
      this.updateCenter();
    });
  }

  private updateCenter(): void {
    if (!this.map) return;
    this.map.setView(this.centroid, 13);
    if (this.userMarker) this.userMarker.setLatLng(this.centroid);
    this.currentPage = 1;
    this.drawRadius();
    this.loadNearbyItems();
  }

  // ── Map init ──────────────────────────────────────────────────────────────

  private initMap(): void {
    if (this.map) return;

    this.map = L.map('map', { center: this.centroid, zoom: 13, zoomControl: true });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18, minZoom: 3, attribution: '&copy; OpenStreetMap',
    }).addTo(this.map);

    const userIcon = L.divIcon({
      html: `<div class="user-pin">📍</div>`,
      className: '', iconSize: [32, 32], iconAnchor: [16, 32],
    });
    this.userMarker = L.marker(this.centroid, { icon: userIcon })
      .addTo(this.map).bindPopup('Your location');

    this.map.on('click', (e: L.LeafletMouseEvent) => {
      this.centroid          = [e.latlng.lat, e.latlng.lng];
      this.locationQuery     = '';
      this.showSuggestions   = false;
      if (this.userMarker) this.userMarker.setLatLng(this.centroid);
      this.currentPage = 1;
      this.drawRadius();
      this.loadNearbyItems();
      this.cdr.detectChanges();
    });

    this.drawRadius();
    this.loadNearbyItems();
    this.cdr.detectChanges();
  }

  // ── Items ─────────────────────────────────────────────────────────────────

  loadNearbyItems(): void {
    if (!this.map) return;
    if (this.isInitialLoad) { this.isLoading = true; this.cdr.detectChanges(); }

    const [lat, lon] = this.centroid as [number, number];

    const filter: ItemFilter = {
      ...(this.searchQuery.trim() && { search: this.searchQuery.trim() }),
      ...(this.selectedCategory    && { categoryId: this.categoryIdMap[this.selectedCategory] }),
      ...(this.availableOnly       && { availability: ItemAvailability.Available }),
      ...(this.freeOnly            && { isFree: true }),
    };
    const request: PagedRequest = {
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
    };

    this.itemService.getNearby(lat, lon, this.radiusKm, filter, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.items         = res.data?.items ?? [];
          this.totalCount    = res.data?.totalCount ?? 0;
          this.isLoading     = false;
          this.isInitialLoad = false;
          this.placeMarkers();
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading     = false;
          this.isInitialLoad = false;
          this.cdr.detectChanges();
        },
      });
  }

  onRadiusChange(): void {
    this.currentPage = 1;
    this.drawRadius();
    this.loadNearbyItems();
  }

  // ── Map drawing ───────────────────────────────────────────────────────────

  private drawRadius(): void {
    if (!this.map) return;
    if (this.radiusCircle) this.radiusCircle.remove();
    this.radiusCircle = L.circle(this.centroid, {
      radius: this.radiusKm * 1000,
      color: '#fbbf24', fillColor: '#fbbf24',
      fillOpacity: 0.05, weight: 2, dashArray: '6 4',
    }).addTo(this.map);
    this.map.fitBounds(this.radiusCircle.getBounds(), { padding: [40, 40] });
  }

  private placeMarkers(): void {
    this.markers.forEach(m => m.remove());
    this.markers = [];

    this.items.forEach(item => {
      if (item.pickupLatitude == null || item.pickupLongitude == null) return;

      const isSelected = this.selectedItem?.id === item.id;
      const emoji      = getCategoryEmoji(item.categoryName);
      const price      = item.isFree ? 'Free' : `${item.pricePerDay} kr`;

      const icon = L.divIcon({
        html: `<div class="item-pin${item.isFree ? ' item-pin--free' : ''}${isSelected ? ' item-pin--selected' : ''}">
                 <span class="item-pin-emoji">${emoji}</span>
                 <span class="item-pin-price">${price}</span>
               </div>`,
        className: '', iconSize: [80, 32], iconAnchor: [40, 32],
      });

      const photoHtml = item.mainPhotoUrl
        ? `<img src="${item.mainPhotoUrl}" class="popup-img" alt="${item.title}" />`
        : `<div class="popup-img popup-img--fallback">${emoji}</div>`;

      const ratingHtml = item.averageRating
        ? `<span class="popup-rating"><span class="popup-star">★</span> ${item.averageRating.toFixed(1)}${item.totalReviews ? ` (${item.totalReviews})` : ''}</span>`
        : `<span class="popup-rating popup-rating--new">New</span>`;

      const availLabel = getAvailabilityLabel(item.availability);
      const availClass = item.availability === ItemAvailability.Available ? 'avail-available'
                       : item.availability === ItemAvailability.OnRent    ? 'avail-rented'
                       : 'avail-unavailable';

      const verHtml = item.requiresVerification
        ? `<div class="popup-verify">⚠️ Verification required</div>`
        : '';

      const ownerInitials = (item.ownerName ?? '').split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2);
      const avatarHtml = item.ownerAvatarUrl
        ? `<img src="${item.ownerAvatarUrl}" class="popup-avatar" />`
        : `<div class="popup-avatar popup-avatar--initials">${ownerInitials}</div>`;

      const descHtml = item.description
        ? `<p class="popup-desc">${item.description}</p>`
        : '';

      const popupHtml = `
        <div class="map-popup">
          ${photoHtml}
          <div class="popup-body">
            <div class="popup-top-row">
              <span class="popup-avail ${availClass}">${availLabel}</span>
              ${ratingHtml}
            </div>
            <h4 class="popup-title">${item.title}</h4>
            <p class="popup-location">📍 ${item.pickupAddress ?? ''}</p>
            ${descHtml}
            <div class="popup-meta-row">
              <div class="popup-owner">
                ${avatarHtml}
                <span class="popup-owner-name">${item.ownerName ?? ''}</span>
              </div>
              <span class="popup-condition">${item.condition ?? ''}</span>
            </div>
            <div class="popup-price-row">
              <span class="popup-price">${item.isFree ? 'Free' : item.pricePerDay + ' kr/day'}</span>
              ${verHtml}
            </div>
            <a class="popup-btn" href="/items/${item.slug}">View item →</a>
          </div>
        </div>`;

      const marker = L.marker([item.pickupLatitude, item.pickupLongitude], { icon })
        .addTo(this.map)
        .bindPopup(popupHtml, { maxWidth: 260, minWidth: 240, className: 'dark-popup' })
        .on('click', () => {
          this.selectedItem = this.selectedItem?.id === item.id ? null : item;
          this.cdr.detectChanges();
          this.placeMarkers();
        });

      this.markers.push(marker);
    });
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  onSearch(): void { this.searchSubject.next(this.searchQuery); }

  selectCategory(name: string | null): void {
    this.selectedCategory = name; this.currentPage = 1; this.loadNearbyItems();
  }

  onSortChange(value: string): void {
    this.sortLabel = value;
    switch (value) {
      case 'newest':     this.sortBy = 'createdAt';     this.sortDescending = true;  break;
      case 'oldest':     this.sortBy = 'createdAt';     this.sortDescending = false; break;
      case 'rating':     this.sortBy = 'averageRating'; this.sortDescending = true;  break;
      case 'az':         this.sortBy = 'title';         this.sortDescending = false; break;
      case 'za':         this.sortBy = 'title';         this.sortDescending = true;  break;
      case 'price_asc':  this.sortBy = 'pricePerDay';   this.sortDescending = false; break;
      case 'price_desc': this.sortBy = 'pricePerDay';   this.sortDescending = true;  break;
    }
    this.currentPage = 1; this.loadNearbyItems();
  }

  onAvailableToggle(): void { this.availableOnly = !this.availableOnly; this.currentPage = 1; this.loadNearbyItems(); }
  onFreeToggle():      void { this.freeOnly      = !this.freeOnly;      this.currentPage = 1; this.loadNearbyItems(); }

  // ── Category strip ────────────────────────────────────────────────────────

  private setupCategoryArrows(): void {
    if (!this.categoryStrip) return;
    const el = this.categoryStrip.nativeElement;
    const update = () => {
      this.showLeftArrow  = el.scrollLeft > 0;
      this.showRightArrow = el.scrollLeft + el.clientWidth < el.scrollWidth - 1;
      this.cdr.detectChanges();
    };
    setTimeout(() => update(), 100);
    el.addEventListener('scroll', update);
    window.addEventListener('resize', update);
  }

  scrollCategories(dir: 'left' | 'right'): void {
    this.categoryStrip.nativeElement.scrollBy({ left: dir === 'right' ? 220 : -220, behavior: 'smooth' });
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  get totalPages(): number    { return getTotalPages(this.totalCount, this.pageSize); }
  get pageNumbers(): number[] { return getPageNumbers(this.currentPage, this.totalPages); }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadNearbyItems();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getCategoryEmoji(n: string): string { return getCategoryEmoji(n); }
  getConditionClass(c: ItemCondition): string { return getConditionClass(c); }
  getAvailabilityClass(a: ItemAvailability): string { return getAvailabilityClass(a); }
  getAvailabilityLabel(a: ItemAvailability): string { return getAvailabilityLabel(a); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}