import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LocationStore } from '../../../core/location/store/location.store';
import { LocationService } from '../../../core/location/services/location.service';

interface CityResult {
  city: string;
  lat: number;
  lon: number;
}

@Component({
  selector: 'app-location',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './location.html',
  styleUrl: './location.scss',
})
export class LocationComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(LocationStore);
  private readonly locationService = inject(LocationService);

  readonly city = this.store.city;
  readonly coordinates = this.store.coordinates;
  readonly isDetecting = this.store.isDetecting;
  readonly error = this.store.error;

  searchForm!: FormGroup;
  searchResults = signal<CityResult[]>([]);
  isSearching = signal(false);

  ngOnInit(): void {
    this.searchForm = this.fb.group({
      query: ['', [Validators.required, Validators.minLength(2)]],
    });
  }

  detectLocation(): void {
    this.locationService.detectCurrentLocation().subscribe({
      error: () => {},
    });
  }

  onSearch(): void {
    if (this.searchForm.invalid) return;

    this.isSearching.set(true);
    this.locationService.searchCity(this.searchForm.value.query).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
      },
      error: () => this.isSearching.set(false),
    });
  }

  selectCity(result: CityResult): void {
    this.store.setLocation(result.city, { lat: result.lat, lon: result.lon });
    this.searchResults.set([]);
    this.searchForm.reset();
  }
}