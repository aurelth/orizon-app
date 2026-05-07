import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { WeatherCardComponent } from './weather-card';

describe('WeatherCardComponent', () => {
  let component: WeatherCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new WeatherCardComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve retornar precipitationHours vazio quando weather não tem dados', () => {
    expect(component.precipitationHours()).toEqual([]);
  });

  it('deve retornar maxPrecipitation 1 quando não há precipitação', () => {
    expect(component.maxPrecipitation()).toBe(1);
  });

  it('deve formatar hora corretamente', () => {
    expect(component.formatHour(8)).toBe('8h');
    expect(component.formatHour(14)).toBe('14h');
  });
});