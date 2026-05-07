import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AISummary } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-ai-suggestions-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-suggestions-card.html',
  styleUrl: './ai-suggestions-card.scss',
})
export class AiSuggestionsCardComponent {
  readonly aiSummary = input<AISummary>({} as AISummary);
}