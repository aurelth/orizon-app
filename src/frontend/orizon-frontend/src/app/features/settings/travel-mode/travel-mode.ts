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
  selector: 'app-travel-mode',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './travel-mode.html',
  styleUrl: './travel-mode.scss',
})
export class TravelModeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(LocationStore);
  private readonly locationService = inject(LocationService);

  readonly travelMode = this.store.travelMode;
  readonly travelCity = this.store.travelCity;
  readonly travelCoordinates = this.store.travelCoordinates;
  readonly baseCity = this.store.city;

  searchForm!: FormGroup;
  searchResults = signal<CityResult[]>([]);
  isSearching = signal(false);
  selectedResult = signal<CityResult | null>(null);

  ngOnInit(): void {
    this.searchForm = this.fb.group({
      query: ['', [Validators.required, Validators.minLength(2)]],
    });
  }

  toggleTravelMode(): void {
    if (this.travelMode()) {
      this.store.disableTravelMode();
      this.selectedResult.set(null);
      this.searchResults.set([]);
      this.searchForm.reset();
    }
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
    this.selectedResult.set(result);
    this.searchResults.set([]);
    this.searchForm.reset();
  }

  enableTravelMode(): void {
    const result = this.selectedResult();
    if (!result) return;
    this.store.enableTravelMode(result.city, { lat: result.lat, lon: result.lon });
  }
}