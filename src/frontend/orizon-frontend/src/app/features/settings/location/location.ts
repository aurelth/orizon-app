import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LocationStore } from '../../../core/location/store/location.store';
import { LocationService } from '../../../core/location/services/location.service';
import { ToastService } from '../../../core/toast/toast.service';

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
  private readonly toast = inject(ToastService);

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

    this.locationService.getLocation().subscribe();
  }

  detectLocation(): void {
    this.locationService.detectCurrentLocation().subscribe({
      next: () => this.toast.success('Localização detectada com sucesso.'),
      error: () => this.toast.error('Não foi possível detectar a localização.'),
    });
  }

  onSearch(): void {
    if (this.searchForm.invalid) return;

    this.isSearching.set(true);
    this.locationService.searchCity(this.searchForm.value.query).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
        if (results.length === 0) {
          this.toast.info('Nenhuma cidade encontrada.');
        }
      },
      error: () => {
        this.isSearching.set(false);
        this.toast.error('Erro ao buscar cidades.');
      },
    });
  }

  selectCity(result: CityResult): void {
    this.store.setLocation(result.city, { lat: result.lat, lon: result.lon });
    this.searchResults.set([]);
    this.searchForm.reset();

    this.locationService.saveLocation(result.city, result.lat, result.lon).subscribe({
      next: () => this.toast.success(`Localização definida para ${result.city}.`),
      error: () => this.toast.error('Erro ao salvar localização.'),
    });
  }
}