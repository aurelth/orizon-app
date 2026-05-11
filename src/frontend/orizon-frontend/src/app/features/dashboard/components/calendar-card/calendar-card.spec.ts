import { TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { CalendarCardComponent } from './calendar-card';
import { CalendarEvent } from '../../../../core/briefing/models/briefing.model';

const makeEvent = (i: number): CalendarEvent => ({
  title: `Event ${i}`,
  startTime: '2026-05-10T10:00:00Z',
  endTime: '2026-05-10T10:30:00Z',
  participants: [],
  meetLink: null,
  description: null,
  conflictsWithRain: false,
});

describe('CalendarCardComponent', () => {
  let component: CalendarCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
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

  it('deve mostrar apenas 4 eventos por padrão', () => {
    const events = [1, 2, 3, 4, 5].map(makeEvent);
    TestBed.runInInjectionContext(() => {
      component = new CalendarCardComponent();
    });
    (component as any).events = () => events;

    expect(component.visibleEvents().length).toBe(4);
  });

  it('não deve mostrar botão Ver mais quando há 4 ou menos eventos', () => {
    const events = [1, 2, 3, 4].map(makeEvent);
    TestBed.runInInjectionContext(() => {
      component = new CalendarCardComponent();
    });
    (component as any).events = () => events;

    expect(component.hasMore()).toBe(false);
  });

  it('deve mostrar botão Ver mais quando há mais de 4 eventos', () => {
    const events = [1, 2, 3, 4, 5].map(makeEvent);
    TestBed.runInInjectionContext(() => {
      component = new CalendarCardComponent();
    });
    (component as any).events = () => events;

    expect(component.hasMore()).toBe(true);
  });

  it('deve mostrar todos os eventos ao chamar toggleShowAll', () => {
    const events = [1, 2, 3, 4, 5].map(makeEvent);
    TestBed.runInInjectionContext(() => {
      component = new CalendarCardComponent();
    });
    (component as any).events = () => events;

    component.toggleShowAll();
    expect(component.visibleEvents().length).toBe(5);
  });

  it('deve voltar para 4 eventos ao chamar toggleShowAll novamente', () => {
    const events = [1, 2, 3, 4, 5].map(makeEvent);
    TestBed.runInInjectionContext(() => {
      component = new CalendarCardComponent();
    });
    (component as any).events = () => events;

    component.toggleShowAll();
    component.toggleShowAll();
    expect(component.visibleEvents().length).toBe(4);
  });

  it('showAll deve inicializar como false', () => {
    expect(component.showAll()).toBe(false);
  });
});