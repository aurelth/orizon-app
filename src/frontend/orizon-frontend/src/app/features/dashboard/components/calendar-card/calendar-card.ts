import { Component, input } from '@angular/core';
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

  formatDuration(start: string, end: string): string {
    const s = new Date(start);
    const e = new Date(end);
    const mins = Math.round((e.getTime() - s.getTime()) / 60000);
    return mins >= 60 ? `${Math.floor(mins / 60)}h${mins % 60 ? ` ${mins % 60}min` : ''}` : `${mins}min`;
  }
}