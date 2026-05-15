import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService, TrelloBoard, TrelloList } from '../../../core/integrations/services/trello-integration.service';
import { BriefingService } from '../../../core/briefing/services/briefing.service';

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
  private readonly briefingService = inject(BriefingService);
  private readonly route = inject(ActivatedRoute);

  readonly googleConnected = this.store.googleConnected;
  readonly trelloConnected = this.store.trelloConnected;
  readonly trelloBoards = this.store.trelloBoards;
  readonly isLoadingGoogle = this.store.isLoadingGoogle;
  readonly isLoadingTrello = this.store.isLoadingTrello;
  readonly error = this.store.error;
  readonly activeBoardIds = this.store.activeBoardIds;

  trelloForm!: FormGroup;
  showTrelloForm = false;
  showBoardSelector = false;
  expandedBoard: TrelloBoard | null = null;
  selectedTodayList: TrelloList | null = null;
  selectedInProgressList: TrelloList | null = null;
  confirmRemoveBoardId: string | null = null;
  isSavingBoard = signal(false);
  isRemovingBoard = signal(false);
  isDisconnectingTrello = signal(false);
  confirmDisconnectTrello = false;

  ngOnInit(): void {
    this.trelloForm = this.fb.group({
      apiKey: ['', [Validators.required, Validators.minLength(32)]],
      token: ['', [Validators.required, Validators.minLength(64)]],
    });

    this.googleService.getStatus().subscribe();
    this.trelloService.getStatus().subscribe({
      next: () => {
        if (this.trelloConnected()) {
          this.trelloService.getConfig().subscribe();
        }
      }
    });

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

  toggleBoardSelector(): void {
    this.showBoardSelector = !this.showBoardSelector;
    if (this.showBoardSelector && this.trelloBoards().length === 0) {
      this.trelloService.getBoards().subscribe();
    }
    if (!this.showBoardSelector) {
      this.expandedBoard = null;
      this.selectedTodayList = null;
      this.selectedInProgressList = null;
      this.confirmRemoveBoardId = null;
    }
  }

  isFieldInvalid(form: FormGroup, field: string): boolean {
    const control = form.get(field);
    return !!(control?.invalid && control?.touched);
  }

  isBoardActive(boardId: string): boolean {
    return this.activeBoardIds().includes(boardId);
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
        this.showBoardSelector = true;
        this.trelloService.getBoards(apiKey, token).subscribe();
      },
    });
  }

  onBoardExpand(board: TrelloBoard): void {
    if (this.expandedBoard?.boardId === board.boardId) {
      this.expandedBoard = null;
      this.selectedTodayList = null;
      this.selectedInProgressList = null;
    } else {
      this.expandedBoard = board;
      this.selectedTodayList = null;
      this.selectedInProgressList = null;
    }
  }

  onTodayListSelect(list: TrelloList): void {
    this.selectedTodayList = list;
  }

  onInProgressListSelect(list: TrelloList): void {
    this.selectedInProgressList = list;
  }

  onConfirmAdd(): void {
    if (!this.expandedBoard || !this.selectedTodayList || !this.selectedInProgressList) return;

    this.isSavingBoard.set(true);
    this.trelloService.saveBoardConfig({
      boardId: this.expandedBoard.boardId,
      boardName: this.expandedBoard.name,
      boardColor: this.expandedBoard.color,
      todayListId: this.selectedTodayList.listId,
      todayListName: this.selectedTodayList.name,
      inProgressListId: this.selectedInProgressList.listId,
      inProgressListName: this.selectedInProgressList.name,
    }).subscribe({
      next: () => {
        this.isSavingBoard.set(false);
        this.expandedBoard = null;
        this.selectedTodayList = null;
        this.selectedInProgressList = null;
        this.regenerateBriefing();
      },
      error: () => this.isSavingBoard.set(false),
    });
  }

  onRequestRemove(boardId: string): void {
    this.confirmRemoveBoardId = boardId;
  }

  onCancelRemove(): void {
    this.confirmRemoveBoardId = null;
  }

  onConfirmRemove(): void {
    if (!this.confirmRemoveBoardId) return;

    this.isRemovingBoard.set(true);
    const boardId = this.confirmRemoveBoardId;
    this.trelloService.removeBoardConfig(boardId).subscribe({
      next: () => {
        this.isRemovingBoard.set(false);
        this.confirmRemoveBoardId = null;
        if (this.expandedBoard?.boardId === boardId) {
          this.expandedBoard = null;
          this.selectedTodayList = null;
          this.selectedInProgressList = null;
        }
        this.regenerateBriefing();
      },
      error: () => this.isRemovingBoard.set(false),
    });
  }

  onRequestDisconnectTrello(): void {
    this.confirmDisconnectTrello = true;
  }

  onCancelDisconnectTrello(): void {
    this.confirmDisconnectTrello = false;
  }

  onConfirmDisconnectTrello(): void {
    this.isDisconnectingTrello.set(true);
    this.trelloService.disconnect().subscribe({
      next: () => {
        this.isDisconnectingTrello.set(false);
        this.confirmDisconnectTrello = false;
        this.showTrelloForm = false;
        this.showBoardSelector = false;
        this.expandedBoard = null;
        this.selectedTodayList = null;
        this.selectedInProgressList = null;
        this.confirmRemoveBoardId = null;
        this.trelloForm.reset();
      },
      error: () => this.isDisconnectingTrello.set(false),
    });
  }

  private regenerateBriefing(): void {
    this.briefingService.generateBriefing().subscribe();
  }
}