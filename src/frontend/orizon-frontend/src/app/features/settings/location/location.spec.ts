import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LocationComponent } from './location';
import { LocationStore } from '../../../core/location/store/location.store';
import { LocationService } from '../../../core/location/services/location.service';
import { ToastService } from '../../../core/toast/toast.service';
import { of, throwError } from 'rxjs';

describe('LocationComponent', () => {
  let component: LocationComponent;
  let locationService: jest.Mocked<Partial<LocationService>>;
  let toastService: jest.Mocked<Partial<ToastService>>;
  let store: InstanceType<typeof LocationStore>;

  const mockResult = { city: 'Blumenau', lat: -26.9194, lon: -49.0661 };

  beforeEach(async () => {
    locationService = {
      detectCurrentLocation: jest.fn(),
      searchCity: jest.fn(),
      getLocation: jest.fn().mockReturnValue(of(null)),
      saveLocation: jest.fn().mockReturnValue(of(void 0)),
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
    component = TestBed.runInInjectionContext(() => new LocationComponent());
    component.ngOnInit();
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar searchForm com query vazia', () => {
    expect(component.searchForm.get('query')?.value).toBe('');
  });

  it('deve chamar detectCurrentLocation ao detectar localização', () => {
    (locationService.detectCurrentLocation as jest.Mock).mockReturnValue(of(mockResult));
    component.detectLocation();
    expect(locationService.detectCurrentLocation).toHaveBeenCalled();
  });

  it('deve chamar toast.success após detectar localização', () => {
    (locationService.detectCurrentLocation as jest.Mock).mockReturnValue(of(mockResult));
    component.detectLocation();
    expect(toastService.success).toHaveBeenCalledWith('Localização detectada com sucesso.');
  });

  it('deve chamar toast.error quando detecção falhar', () => {
    (locationService.detectCurrentLocation as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.detectLocation();
    expect(toastService.error).toHaveBeenCalledWith(
      'Não foi possível detectar a localização.');
  });

  it('não deve buscar quando searchForm inválido', () => {
    component.onSearch();
    expect(locationService.searchCity).not.toHaveBeenCalled();
  });

  it('deve buscar cidades com query válida', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(of([mockResult]));
    component.searchForm.get('query')?.setValue('Blumenau');
    component.onSearch();
    expect(locationService.searchCity).toHaveBeenCalledWith('Blumenau');
  });

  it('deve popular searchResults após busca bem-sucedida', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(of([mockResult]));
    component.searchForm.get('query')?.setValue('Blumenau');
    component.onSearch();
    expect(component.searchResults()).toHaveLength(1);
  });

  it('deve chamar toast.info quando nenhuma cidade for encontrada', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(of([]));
    component.searchForm.get('query')?.setValue('xyz');
    component.onSearch();
    expect(toastService.info).toHaveBeenCalledWith('Nenhuma cidade encontrada.');
  });

  it('deve limpar resultados e atualizar store ao selecionar cidade', () => {
    component.searchResults.set([mockResult]);
    component.selectCity(mockResult);
    expect(component.searchResults()).toHaveLength(0);
    expect(store.city()).toBe('Blumenau');
  });

  it('deve chamar toast.success ao selecionar cidade', () => {
    component.selectCity(mockResult);
    expect(toastService.success).toHaveBeenCalledWith(
      'Localização definida para Blumenau.');
  });

  it('deve definir isSearching como false após erro na busca', () => {
    (locationService.searchCity as jest.Mock).mockReturnValue(
      throwError(() => new Error('error'))
    );
    component.searchForm.get('query')?.setValue('Blumenau');
    component.onSearch();
    expect(component.isSearching()).toBe(false);
  });
});