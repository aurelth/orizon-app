import { Component, input } from '@angular/core';
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
}