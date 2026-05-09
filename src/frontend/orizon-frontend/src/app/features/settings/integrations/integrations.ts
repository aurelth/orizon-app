import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService, TrelloBoard, TrelloList } from '../../../core/integrations/services/trello-integration.service';

@Component({
  selector: 'app-integrations',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './integrations.html',
  styleUrl: './integrations.scss',
})
export class IntegrationsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(IntegrationsStore);
  private readonly googleService = inject(GoogleIntegrationService);
  private readonly trelloService = inject(TrelloIntegrationService);
  private readonly route = inject(ActivatedRoute);

  readonly googleConnected = this.store.googleConnected;
  readonly trelloConnected = this.store.trelloConnected;
  readonly trelloBoards = this.store.trelloBoards;
  readonly isLoadingGoogle = this.store.isLoadingGoogle;
  readonly isLoadingTrello = this.store.isLoadingTrello;
  readonly error = this.store.error;

  trelloForm!: FormGroup;
  showTrelloForm = false;
  selectedBoard: TrelloBoard | null = null;
  selectedTodayList: TrelloList | null = null;
  selectedInProgressList: TrelloList | null = null;
  isSavingBoard = signal(false);

  ngOnInit(): void {
    this.trelloForm = this.fb.group({
      apiKey: ['', [Validators.required, Validators.minLength(32)]],
      token: ['', [Validators.required, Validators.minLength(64)]],
    });

    this.googleService.getStatus().subscribe();

    const googleParam = this.route.snapshot.queryParamMap.get('google');
    if (googleParam === 'success') {
      this.store.setGoogleConnected(true);
    } else if (googleParam === 'error') {
      this.store.setError('Falha ao conectar com o Google. Tente novamente.');
    }
  }

  connectGoogle(): void {
    this.googleService.redirectToGoogle();
  }

  toggleTrelloForm(): void {
    this.showTrelloForm = !this.showTrelloForm;
    this.store.clearError();
  }

  isFieldInvalid(form: FormGroup, field: string): boolean {
    const control = form.get(field);
    return !!(control?.invalid && control?.touched);
  }

  onTrelloSubmit(): void {
    if (this.trelloForm.invalid) {
      this.trelloForm.markAllAsTouched();
      return;
    }

    const { apiKey, token } = this.trelloForm.value;
    this.trelloService.connect(apiKey, token).subscribe({
      next: () => {
        this.showTrelloForm = false;
        this.trelloService.getBoards(apiKey, token).subscribe();
      },
    });
  }

  onBoardSelect(board: TrelloBoard): void {
    this.selectedBoard = board;
    this.selectedTodayList = null;
    this.selectedInProgressList = null;
  }

  onTodayListSelect(list: TrelloList): void {
    this.selectedTodayList = list;
  }

  onInProgressListSelect(list: TrelloList): void {
    this.selectedInProgressList = list;
  }

  onSaveBoardConfig(): void {
    if (!this.selectedBoard) return;

    this.isSavingBoard.set(true);
    this.trelloService.saveBoardConfig({
      boardId: this.selectedBoard.boardId,
      boardName: this.selectedBoard.name,
      boardColor: this.selectedBoard.color,
      todayListId: this.selectedTodayList?.listId,
      todayListName: this.selectedTodayList?.name,
      inProgressListId: this.selectedInProgressList?.listId,
      inProgressListName: this.selectedInProgressList?.name,
    }).subscribe({
      next: () => this.isSavingBoard.set(false),
      error: () => this.isSavingBoard.set(false),
    });
  }
}