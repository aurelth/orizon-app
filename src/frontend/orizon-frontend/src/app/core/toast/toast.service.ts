import { Injectable, inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastr = inject(ToastrService);

  success(message: string, title = 'Sucesso'): void {
    this.toastr.success(message, title);
  }

  error(message: string, title = 'Erro'): void {
    this.toastr.error(message, title);
  }

  info(message: string, title = 'Info'): void {
    this.toastr.info(message, title);
  }

  warning(message: string, title = 'Atenção'): void {
    this.toastr.warning(message, title);
  }
}