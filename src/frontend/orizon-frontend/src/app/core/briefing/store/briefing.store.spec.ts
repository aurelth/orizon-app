import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BriefingStore } from './briefing.store';
import { BriefingResult } from '../models/briefing.model';

describe('BriefingStore', () => {
  let store: InstanceType<typeof BriefingStore>;

  const mockBriefing: BriefingResult = {
    briefingId: 'abc-123',
    date: '2026-05-06',
    userName: 'Aurel',
    weather: {
      currentTemperature: 22,
      minTemperature: 18,
      maxTemperature: 26,
      description: 'Ensolarado',
      weatherEmoji: '☀️',
      humidity: 60,
      windSpeed: 10,
      locationName: 'Blumenau',
      hourlyPrecipitation: {},
      rainStartHour: null,
      rainEndHour: null,
      willRain: false,
    },
    emails: [],
    calendarEvents: [],
    trelloTasks: null,
    aiSummary: {
      greeting: 'Bom dia, Aurel!',
      weatherSummary: 'Dia ensolarado.',
      suggestions: 'Ótimo dia para trabalhar.',
      priorityTask: 'Revisar PR',
      actionChips: ['Daily às 10h'],
    },
    generatedAt: '2026-05-06T06:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        BriefingStore,
      ],
    });

    store = TestBed.inject(BriefingStore);
  });

  it('deve inicializar com estado vazio', () => {
    expect(store.briefing()).toBeNull();
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('deve definir briefing corretamente', () => {
    store.setBriefing(mockBriefing);
    expect(store.briefing()).toEqual(mockBriefing);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
    expect(store.lastUpdated()).not.toBeNull();
  });

  it('deve definir isLoading corretamente', () => {
    store.setLoading(true);
    expect(store.isLoading()).toBe(true);
    store.setLoading(false);
    expect(store.isLoading()).toBe(false);
  });

  it('deve definir isConnecting corretamente', () => {
    store.setConnecting(true);
    expect(store.isConnecting()).toBe(true);
    store.setConnecting(false);
    expect(store.isConnecting()).toBe(false);
  });

  it('deve definir erro e resetar isLoading', () => {
    store.setLoading(true);
    store.setError('Briefing não encontrado.');
    expect(store.error()).toBe('Briefing não encontrado.');
    expect(store.isLoading()).toBe(false);
  });

  it('deve limpar erro', () => {
    store.setError('Erro qualquer');
    store.clearError();
    expect(store.error()).toBeNull();
  });
});