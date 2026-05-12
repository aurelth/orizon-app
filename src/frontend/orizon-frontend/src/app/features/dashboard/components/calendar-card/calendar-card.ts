import { Component, input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-calendar-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendar-card.html',
  styleUrl: './calendar-card.scss',
})
export class CalendarCardComponent {
  readonly events = input<CalendarEvent[]>([]);

  readonly showAll = signal(false);

  readonly regularEvents = computed(() =>
    this.events().filter(e => !e.isBirthday)
  );

  readonly birthdays = computed(() =>
    this.events().filter(e => e.isBirthday)
  );

  readonly visibleEvents = computed(() => {
    const all = this.events();
    return this.showAll() ? all : all.slice(0, 4);
  });

  readonly hasMore = computed(() => this.events().length > 4);

  toggleShowAll(): void {
    this.showAll.update(v => !v);
  }

  formatDuration(start: string, end: string): string {
    const s = new Date(start);
    const e = new Date(end);
    const mins = Math.round((e.getTime() - s.getTime()) / 60000);
    return mins >= 60 ? `${Math.floor(mins / 60)}h${mins % 60 ? ` ${mins % 60}min` : ''}` : `${mins}min`;
  }
}