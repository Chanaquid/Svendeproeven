import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone  } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { RegisterUserRequestDto } from '../../dtos/userDTO';
import { AuthService } from '../../services/authService';
import { UploadThingService } from '../../services/UploadThingService'; 

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})

export class Register {

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
  private avatarFile: File | null = null;
  private searchTimeout: any;

  constructor(
    private authService: AuthService,
    private uploadService: UploadThingService,
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
      const apiKey = '6efe16ed3bb047b8975d6f4738a471a9';
      const url = `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(value)}&limit=5&apiKey=${apiKey}`;

      fetch(url)
        .then(res => res.json())
        .then(data => {
          this.suggestions = data.features ?? [];
          this.showSuggestions = true;
          console.log(data)

          this.cdr.detectChanges();
        });
    }, 400);
  }

  //avatar pic
  onAvatarSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];

  if (!file) return;

  if (file.size > 4 * 1024 * 1024) {
    this.errorMessage = 'Image must be under 4MB.';
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
      this.errorMessage = 'Passwords do not match';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Upload avatar first if selected
    if (this.avatarFile) {
      try {
        this.isUploadingAvatar = true;
        this.dto.avatarUrl = await this.uploadService.uploadAvatar(this.avatarFile);
        this.isUploadingAvatar = false;
      } catch {
        this.errorMessage = 'Image upload failed. Please try again.';
        this.isLoading = false;
        this.isUploadingAvatar = false;
        this.cdr.detectChanges();
        return;
      }
    }

    this.authService.register(this.dto).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Account created! Please check your email to confirm your account.';
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
        this.errorMessage = err.error?.message ?? 'Registration failed. Please try again.';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }



}