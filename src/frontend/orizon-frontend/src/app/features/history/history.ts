import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  BriefingService,
  BriefingHistoryResult,
  UserStats,
} from '../../core/briefing/services/briefing.service';

export type PeriodFilter = 'week' | 'month' | 'all';

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
  readonly isLoadingStats = signal(true);
  readonly history = signal<BriefingHistoryResult | null>(null);
  readonly stats = signal<UserStats | null>(null);
  readonly error = signal<string | null>(null);

  currentPage = 1;
  readonly pageSize = 10;
  activePeriod: PeriodFilter = 'all';

  ngOnInit(): void {
    this.loadStats();
    this.loadHistory();
  }

  loadStats(): void {
    this.isLoadingStats.set(true);
    this.briefingService.getStats().subscribe({
      next: (stats) => {
        this.stats.set(stats);
        this.isLoadingStats.set(false);
      },
      error: () => this.isLoadingStats.set(false),
    });
  }

  loadHistory(page = 1): void {
    this.isLoading.set(true);
    this.currentPage = page;

    const { dateFrom, dateTo } = this.getPeriodDates(this.activePeriod);

    this.briefingService.getHistory(page, this.pageSize, dateFrom, dateTo).subscribe({
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

  setPeriodFilter(period: PeriodFilter): void {
    this.activePeriod = period;
    this.loadHistory(1);
  }

  private getPeriodDates(period: PeriodFilter): { dateFrom?: string; dateTo?: string } {
    const today = new Date();
    if (period === 'week') {
      const from = new Date(today);
      from.setDate(today.getDate() - 7);
      return { dateFrom: this.toApiDate(from) };
    }
    if (period === 'month') {
      const from = new Date(today);
      from.setDate(today.getDate() - 30);
      return { dateFrom: this.toApiDate(from) };
    }
    return {};
  }

  private toApiDate(date: Date): string {
    return date.toISOString().split('T')[0];
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

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Generated': return 'Gerado';
      case 'Failed': return 'Falhou';
      default: return 'Pendente';
    }
  }
}