import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BriefingService } from '../../../core/briefing/services/briefing.service';
import { BriefingResult } from '../../../core/briefing/models/briefing.model';
import { WeatherCardComponent } from '../../dashboard/components/weather-card/weather-card';
import { EmailsCardComponent } from '../../dashboard/components/emails-card/emails-card';
import { CalendarCardComponent } from '../../dashboard/components/calendar-card/calendar-card';
import { TrelloCardComponent } from '../../dashboard/components/trello-card/trello-card';
import { AiSuggestionsCardComponent } from '../../dashboard/components/ai-suggestions-card/ai-suggestions-card';

@Component({
  selector: 'app-history-detail',
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
  templateUrl: './history-detail.html',
  styleUrl: './history-detail.scss',
})
export class HistoryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly briefingService = inject(BriefingService);

  readonly isLoading = signal(true);
  readonly briefing = signal<BriefingResult | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const date = this.route.snapshot.paramMap.get('date');
    if (!date) {
      this.router.navigate(['/history']);
      return;
    }

    this.briefingService.getBriefingByDate(date).subscribe({
      next: (result) => {
        this.briefing.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Briefing não encontrado para esta data.');
        this.isLoading.set(false);
      },
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