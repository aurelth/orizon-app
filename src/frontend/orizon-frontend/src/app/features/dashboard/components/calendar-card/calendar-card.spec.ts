import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CalendarCardComponent } from './calendar-card';

describe('CalendarCardComponent', () => {
  let component: CalendarCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new CalendarCardComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar com lista vazia', () => {
    expect(component.events()).toEqual([]);
  });

  it('deve formatar duração em minutos', () => {
    expect(component.formatDuration(
      '2026-05-06T10:00:00Z',
      '2026-05-06T10:30:00Z'
    )).toBe('30min');
  });

  it('deve formatar duração em horas', () => {
    expect(component.formatDuration(
      '2026-05-06T10:00:00Z',
      '2026-05-06T11:00:00Z'
    )).toBe('1h');
  });

  it('deve formatar duração em horas e minutos', () => {
    expect(component.formatDuration(
      '2026-05-06T10:00:00Z',
      '2026-05-06T11:30:00Z'
    )).toBe('1h 30min');
  });
});