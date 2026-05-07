import { Component, input, computed } from '@angular/core';
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

  readonly inProgressTasks = computed(() =>
    this.tasks().filter((t) => t.columnType === 'inprogress')
  );

  readonly todayTasks = computed(() =>
    this.tasks().filter((t) => t.columnType === 'today')
  );
}