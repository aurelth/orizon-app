import { Component, inject, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BriefingStore } from '../../core/briefing/store/briefing.store';
import { BriefingService } from '../../core/briefing/services/briefing.service';
import { WeatherCardComponent } from './components/weather-card/weather-card';
import { EmailsCardComponent } from './components/emails-card/emails-card';
import { CalendarCardComponent } from './components/calendar-card/calendar-card';
import { TrelloCardComponent } from './components/trello-card/trello-card';
import { AiSuggestionsCardComponent } from './components/ai-suggestions-card/ai-suggestions-card';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    WeatherCardComponent,
    EmailsCardComponent,
    CalendarCardComponent,
    TrelloCardComponent,
    AiSuggestionsCardComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
  private readonly briefingService = inject(BriefingService);
  readonly store = inject(BriefingStore);

  readonly briefing = this.store.briefing;
  readonly isLoading = this.store.isLoading;
  readonly error = this.store.error;

  readonly weather = computed(() => this.store.briefing()?.weather);
  readonly weatherSummary = computed(() => this.store.briefing()?.aiSummary?.weatherSummary ?? '');
  readonly emails = computed(() => this.store.briefing()?.emails ?? []);
  readonly calendarEvents = computed(() => this.store.briefing()?.calendarEvents ?? []);
  readonly trelloTasks = computed(() => this.store.briefing()?.trelloTasks ?? null);
  readonly aiSummary = computed(() => this.store.briefing()?.aiSummary);
  readonly isGenerating = signal(false);

  ngOnInit(): void {
    this.briefingService.getTodayBriefing().subscribe();
    this.briefingService.connectSignalR(
      `${environment.apiUrl}/hubs/briefing`
    );
  }

  generateBriefing(): void {
    this.isGenerating.set(true);
    this.briefingService.generateBriefing().subscribe({
      next: () => this.isGenerating.set(false),
      error: () => this.isGenerating.set(false),
    });
  }

  formatDate(date: string): string {
    const d = new Date(date + 'T12:00:00-03:00');
    const weekday = d.toLocaleDateString('pt-BR', { weekday: 'long' });
    const day = d.getDate();
    const month = d.toLocaleDateString('pt-BR', { month: 'long' });

    const weekdayCapitalized = weekday.charAt(0).toUpperCase() + weekday.slice(1);
    const monthCapitalized = month.charAt(0).toUpperCase() + month.slice(1);

    return `${weekdayCapitalized}, ${day} de ${monthCapitalized}`;
  }
}