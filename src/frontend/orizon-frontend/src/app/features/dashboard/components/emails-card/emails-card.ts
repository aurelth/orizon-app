import { Component, input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmailSummary } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-emails-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './emails-card.html',
  styleUrl: './emails-card.scss',
})
export class EmailsCardComponent {
  readonly emails = input<EmailSummary[]>([]);

  readonly showAll = signal(false);

  readonly visibleEmails = computed(() =>
    this.showAll() ? this.emails() : this.emails().slice(0, 3)
  );

  readonly hasMore = computed(() => this.emails().length > 3);

  toggleShowAll(): void {
    this.showAll.update(v => !v);
  }
}