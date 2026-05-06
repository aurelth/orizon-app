import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { IntegrationsStore } from '../../../core/integrations/store/integrations.store';
import { GoogleIntegrationService } from '../../../core/integrations/services/google-integration.service';
import { TrelloIntegrationService, TrelloBoard } from '../../../core/integrations/services/trello-integration.service';

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

  readonly googleConnected = this.store.googleConnected;
  readonly trelloConnected = this.store.trelloConnected;
  readonly trelloBoards = this.store.trelloBoards;
  readonly isLoadingGoogle = this.store.isLoadingGoogle;
  readonly isLoadingTrello = this.store.isLoadingTrello;
  readonly error = this.store.error;

  trelloForm!: FormGroup;
  boardForm!: FormGroup;
  showTrelloForm = false;
  selectedBoard: TrelloBoard | null = null;

  ngOnInit(): void {
    this.trelloForm = this.fb.group({
      apiKey: ['', [Validators.required, Validators.minLength(32)]],
      token: ['', [Validators.required, Validators.minLength(64)]],
    });

    this.boardForm = this.fb.group({
      boardId: ['', Validators.required],
    });
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
        this.trelloService.getBoards().subscribe();
      },
    });
  }

  onBoardSelect(board: TrelloBoard): void {
    this.selectedBoard = board;
    this.boardForm.patchValue({ boardId: board.id });
  }

  onSaveBoardConfig(): void {
    if (this.boardForm.invalid || !this.selectedBoard) return;

    this.trelloService.saveBoardConfig({
      boardId: this.selectedBoard.id,
      listIds: [],
    }).subscribe();
  }
}