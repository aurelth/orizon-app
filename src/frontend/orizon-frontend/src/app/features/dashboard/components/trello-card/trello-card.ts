import { Component, input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TrelloTask } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-trello-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './trello-card.html',
  styleUrl: './trello-card.scss',
})
export class TrelloCardComponent {
  readonly tasks = input<TrelloTask[]>([]);

  readonly showAll = signal(false);

  readonly inProgressTasks = computed(() =>
    this.tasks().filter((t) => t.columnType === 'inprogress')
  );

  readonly todayTasks = computed(() =>
    this.tasks().filter((t) => t.columnType === 'today')
  );

  readonly visibleInProgress = computed(() =>
    this.showAll()
      ? this.inProgressTasks()
      : this.inProgressTasks().slice(0, 4)
  );

  readonly visibleToday = computed(() => {
    const remainingSlots = this.showAll()
      ? this.todayTasks().length
      : Math.max(0, 4 - this.inProgressTasks().length);
    return this.todayTasks().slice(0, remainingSlots);
  });

  readonly hasMore = computed(() => this.tasks().length > 4);

  toggleShowAll(): void {
    this.showAll.update(v => !v);
  }
}