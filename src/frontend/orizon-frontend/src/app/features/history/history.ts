import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { BriefingService, BriefingHistoryResult } from '../../core/briefing/services/briefing.service';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './history.html',
  styleUrl: './history.scss',
})
export class HistoryComponent implements OnInit {
  private readonly briefingService = inject(BriefingService);
  private readonly router = inject(Router);

  readonly isLoading = signal(true);
  readonly history = signal<BriefingHistoryResult | null>(null);
  readonly error = signal<string | null>(null);

  currentPage = 1;
  readonly pageSize = 10;

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(page = 1): void {
    this.isLoading.set(true);
    this.currentPage = page;
    this.briefingService.getHistory(page, this.pageSize).subscribe({
      next: (result) => {
        this.history.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Falha ao carregar histórico.');
        this.isLoading.set(false);
      },
    });
  }

  openBriefing(date: string): void {
    this.router.navigate(['/history', date]);
  }

  formatDate(date: string): string {
    const d = new Date(date + 'T12:00:00-03:00');
    return d.toLocaleDateString('pt-BR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }
}