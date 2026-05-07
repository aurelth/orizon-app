import { Component, inject, OnInit, computed  } from '@angular/core';
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

  ngOnInit(): void {
    this.briefingService.getTodayBriefing().subscribe();
    this.briefingService.connectSignalR(
      `${environment.apiUrl}/hubs/briefing`
    );
  }
}