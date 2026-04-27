import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { RegisterUserRequestDto } from '../../dtos/userDto';
import { AuthService } from '../../services/authService';
import { UploadImageService } from '../../services/uploadImageService';
import { ThemeService } from '../../services/themeService';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  readonly theme = inject(ThemeService);

  dto: RegisterUserRequestDto = {
    fullName: '',
    email: '',
    username: '',
    password: '',
    confirmPassword: '',
    address: '',
    dateOfBirth: '',
    gender: '',
    avatarUrl: undefined,
    latitude: undefined,
    longitude: undefined,
  };

  isLoading = false;
  isUploadingAvatar = false;
  errorMessage = '';
  successMessage = '';
  suggestions: any[] = [];
  showSuggestions = false;
  avatarPreview: string | null = null;
  acceptedTerms = false;
  private avatarFile: File | null = null;
  private searchTimeout: any;

  constructor(
    private authService: AuthService,
    private uploadService: UploadImageService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  onAddressInput(value: string) {
    clearTimeout(this.searchTimeout);
    this.showSuggestions = false;

    if (value.length < 3) {
      this.suggestions = [];
      return;
    }

    this.searchTimeout = setTimeout(() => {
      const apiKey = environment.geoapifyKey;
      const url = `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(value)}&limit=5&apiKey=${apiKey}`;

      fetch(url)
        .then((res) => res.json())
        .then((data) => {
          this.suggestions = data.features ?? [];
          this.showSuggestions = true;
          this.cdr.detectChanges();
        });
    }, 400);
  }

  onAvatarSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (file.size > 4 * 1024 * 1024) {
      this.errorMessage = 'Billedet skal være under 4 MB.';
      input.value = '';
      return;
    }

    this.avatarFile = file;
    this.errorMessage = '';
    this.avatarPreview = URL.createObjectURL(file);
    input.value = '';
  }

  removeAvatar() {
    this.avatarPreview = null;
    this.avatarFile = null;
    this.dto.avatarUrl = undefined;
  }

  selectSuggestion(place: any) {
    const props = place.properties;
    this.dto.address = props.formatted;
    this.dto.latitude = place.geometry.coordinates[1];
    this.dto.longitude = place.geometry.coordinates[0];
    this.suggestions = [];
    this.showSuggestions = false;
    this.cdr.detectChanges();
  }

  async register() {
    if (this.dto.password !== this.dto.confirmPassword) {
      this.errorMessage = 'Adgangskoderne stemmer ikke overens';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.avatarFile) {
      try {
        this.isUploadingAvatar = true;
        const url = await this.uploadService.uploadAvatar(this.avatarFile);
        this.dto.avatarUrl = url;
      } catch (e) {
        console.error('Upload error:', e);
        this.errorMessage = 'Billedoverførsel mislykkedes. Prøv igen.';
        this.isLoading = false;
        return;
      } finally {
        this.isUploadingAvatar = false;
      }
    }

    this.authService.register(this.dto).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Konto oprettet! Tjek din e-mail for at bekræfte din konto.';
        this.dto = {
          fullName: '',
          email: '',
          username: '',
          password: '',
          confirmPassword: '',
          address: '',
          dateOfBirth: '',
          gender: '',
          avatarUrl: undefined,
          latitude: undefined,
          longitude: undefined,
        };
        this.avatarPreview = null;
        this.avatarFile = null;
        this.cdr.detectChanges();
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Registrering mislykkedes. Prøv igen.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
