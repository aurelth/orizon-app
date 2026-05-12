import { Component, input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GoogleTask } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-google-tasks-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './google-tasks-card.html',
  styleUrl: './google-tasks-card.scss',
})
export class GoogleTasksCardComponent {
  readonly tasks = input<GoogleTask[]>([]);

  readonly showAll = signal(false);

  readonly visibleTasks = computed(() =>
    this.showAll() ? this.tasks() : this.tasks().slice(0, 5)
  );

  readonly hasMore = computed(() => this.tasks().length > 5);

  toggleShowAll(): void {
    this.showAll.update(v => !v);
  }

  formatDueDate(dueDate: string | null): string | null {
    if (!dueDate) return null;
    const d = new Date(dueDate);
    const day = String(d.getUTCDate()).padStart(2, '0');
    const month = String(d.getUTCMonth() + 1).padStart(2, '0');
    return `${day}/${month}`;
  }
}