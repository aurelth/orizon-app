import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TravelModeComponent } from './travel-mode';
import { LocationStore } from '../../../core/location/store/location.store';
import { LocationService } from '../../../core/location/services/location.service';
import { ToastService } from '../../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('TravelModeComponent', () => {
  let component: TravelModeComponent;
  let locationService: jest.Mocked<Partial<LocationService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;
  let store: InstanceType<typeof LocationStore>;

  const mockResult = { city: 'Lisboa', lat: 38.7169, lon: -9.1395 };

  beforeEach(async () => {
    locationService = {
      searchCity: jest.fn(),
    };

    toastService = {
      success: jest.fn(),
      error: jest.fn(),
      info: jest.fn(),
    };

    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        LocationStore,
        { provide: LocationService, useValue: locationService },
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    store = TestBed.inject(LocationStore);
    component = TestBed.runInInjectionContext(() => new TravelModeComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar searchForm com query vazia', () => {
    expect(component.searchForm.get('query')?.value).toBe('');
  });

  it('deve inicializar travelMode como false', () => {
    expect(component.travelMode()).toBe(false);
  });

  it('não deve buscar quando searchForm inválido', () => {
    component.onSearch();
    expect(locationService.searchCity).not.toHaveBeenCalled();
  });

  it('deve buscar cidades com query válida', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(of([mockResult]));
    component.searchForm.get('query')?.setValue('Lisboa');
    component.onSearch();
    expect(locationService.searchCity).toHaveBeenCalledWith('Lisboa');
  });

  it('deve popular searchResults após busca bem-sucedida', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(of([mockResult]));
    component.searchForm.get('query')?.setValue('Lisboa');
    component.onSearch();
    expect(component.searchResults()).toHaveLength(1);
  });

  it('deve selecionar cidade e limpar resultados', () => {
    component.searchResults.set([mockResult]);
    component.selectCity(mockResult);
    expect(component.selectedResult()).toEqual(mockResult);
    expect(component.searchResults()).toHaveLength(0);
  });

  it('deve ativar modo viagem e chamar toast.success', () => {
    component.selectedResult.set(mockResult);
    component.enableTravelMode();
    expect(store.travelMode()).toBe(true);
    expect(toastService.success).toHaveBeenCalledWith(
      'Modo viagem ativado para Lisboa.');
  });

  it('não deve ativar modo viagem sem cidade selecionada', () => {
    component.enableTravelMode();
    expect(store.travelMode()).toBe(false);
  });

  it('deve desativar modo viagem e chamar toast.info', () => {
    store.enableTravelMode('Lisboa', { lat: 38.7169, lon: -9.1395 });
    component.toggleTravelMode();
    expect(store.travelMode()).toBe(false);
    expect(toastService.info).toHaveBeenCalledWith('Modo viagem desativado.');
  });

  it('deve definir isSearching como false após erro na busca', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.searchForm.get('query')?.setValue('Lisboa');
    component.onSearch();
    expect(component.isSearching()).toBe(false);
  });
});