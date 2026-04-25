import { AfterViewInit, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as L from 'leaflet';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Navbar } from '../navbar/navbar';
import { ItemService } from '../../services/itemService';
import { ItemListDto } from '../../dtos/itemDto';
import { ItemAvailability, ItemCondition } from '../../dtos/enums';
import { PagedRequest } from '../../dtos/paginationDto';
import { ItemFilter } from '../../dtos/filterDto';
import { Router } from '@angular/router';
import {
  getConditionClass,
  getCategoryEmoji,
  getAvailabilityClass,
  getAvailabilityLabel,
} from '../../utils/item.utils';

@Component({
  selector: 'app-map',
  imports: [CommonModule, FormsModule, Navbar],
  templateUrl: './map.html',
  styleUrl: './map.css',
})
export class Map implements OnInit, AfterViewInit, OnDestroy {

  private map!: L.Map;
  private centroid: L.LatLngExpression = [55.6761, 12.5683];
  private markers: L.Marker[] = [];
  private radiusCircle: L.Circle | null = null;
  private destroy$ = new Subject<void>();

  items: ItemListDto[] = [];
  selectedItem: ItemListDto | null = null;
  isLoading = false;
  errorMessage = '';

  radiusKm = 5;
  totalCount = 0;

  readonly radiusOptions = [1, 2, 5, 10, 20, 50];

  constructor(
    private itemService: ItemService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.getUserLocation();
  }

  ngAfterViewInit(): void {
    setTimeout(() => { if (this.map) this.map.invalidateSize(); }, 200);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ─── Location ─────────────────────────────────────────────────────────────

  private getUserLocation(): void {
    if (!navigator.geolocation) {
      this.initMap();
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.centroid = [position.coords.latitude, position.coords.longitude];
        this.initMap();
      },
      () => {
        this.centroid = [55.6761, 12.5683];
        this.initMap();
      }
    );
  }

  // ─── Map init ─────────────────────────────────────────────────────────────

  private initMap(): void {
    if (this.map) return;

    this.map = L.map('map', {
      center: this.centroid,
      zoom: 13,
      zoomControl: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      minZoom: 3,
      attribution: '&copy; OpenStreetMap'
    }).addTo(this.map);

    // User location marker
    const userIcon = L.divIcon({
      html: `<div class="user-pin">📍</div>`,
      className: '',
      iconSize: [32, 32],
      iconAnchor: [16, 32],
    });

    L.marker(this.centroid, { icon: userIcon })
      .addTo(this.map)
      .bindPopup('Your location');

    this.drawRadiusCircle();
    this.loadNearbyItems();
  }

  // ─── Load items ───────────────────────────────────────────────────────────

  loadNearbyItems(): void {
    if (!this.map) return;
    this.isLoading = true;
    this.errorMessage = '';
    this.selectedItem = null;
    this.cdr.detectChanges();

    const [lat, lon] = this.centroid as [number, number];
    const filter: ItemFilter = {};
    const request: PagedRequest = { page: 1, pageSize: 100, sortBy: 'createdAt', sortDescending: true };

    this.itemService.getNearby(lat, lon, this.radiusKm, filter, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.items = res.data?.items ?? [];
          this.totalCount = res.data?.totalCount ?? 0;
          this.isLoading = false;
          this.drawRadiusCircle();
          this.placeMarkers();
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.errorMessage = err.error?.message ?? 'Failed to load nearby items.';
          this.isLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  onRadiusChange(): void {
    this.drawRadiusCircle();
    this.loadNearbyItems();
  }

  // ─── Map drawing ──────────────────────────────────────────────────────────

  private drawRadiusCircle(): void {
    if (!this.map) return;
    if (this.radiusCircle) { this.radiusCircle.remove(); }

    this.radiusCircle = L.circle(this.centroid, {
      radius: this.radiusKm * 1000,
      color: '#fbbf24',
      fillColor: '#fbbf24',
      fillOpacity: 0.05,
      weight: 2,
      dashArray: '6 4',
    }).addTo(this.map);

    this.map.fitBounds(this.radiusCircle.getBounds(), { padding: [40, 40] });
  }

  private placeMarkers(): void {
    // Clear old markers
    this.markers.forEach(m => m.remove());
    this.markers = [];

    this.items.forEach(item => {
      if (item.pickupLatitude == null || item.pickupLongitude == null) return;

      const icon = L.divIcon({
        html: `
          <div class="item-pin ${item.isFree ? 'item-pin--free' : ''}">
            <span class="item-pin-emoji">${getCategoryEmoji(item.categoryName)}</span>
            <span class="item-pin-price">${item.isFree ? 'Free' : item.pricePerDay + ' kr'}</span>
          </div>`,
        className: '',
        iconSize: [72, 36],
        iconAnchor: [36, 36],
      });

      const marker = L.marker([item.pickupLatitude, item.pickupLongitude], { icon })
        .addTo(this.map)
        .on('click', () => {
  marker.bindPopup(`
    <div>
      <b>${item.title}</b><br/>
      ${item.isFree ? 'Free' : item.pricePerDay + ' kr/day'}<br/>
      <a href="/items/${item.slug}">Open</a>
    </div>
  `).openPopup();
});

      this.markers.push(marker);
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  goToItem(slug: string): void { this.router.navigate(['/items', slug]); }
  getConditionClass(c: ItemCondition): string { return getConditionClass(c); }
  getAvailabilityClass(a: ItemAvailability): string { return getAvailabilityClass(a); }
  getAvailabilityLabel(a: ItemAvailability): string { return getAvailabilityLabel(a); }
  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) ?? '';
  }
}