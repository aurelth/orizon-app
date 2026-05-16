import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';
import { ToastrService } from 'ngx-toastr';

describe('ToastService', () => {
  let service: ToastService;
  let toastr: jest.Mocked<Partial<ToastrService>>;

  beforeEach(() => {
    toastr = {
      success: jest.fn(),
      error: jest.fn(),
      info: jest.fn(),
      warning: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        ToastService,
        { provide: ToastrService, useValue: toastr },
      ],
    });

    service = TestBed.inject(ToastService);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve chamar toastr.success com mensagem e título padrão', () => {
    service.success('Salvo!');
    expect(toastr.success).toHaveBeenCalledWith('Salvo!', 'Sucesso');
  });

  it('deve chamar toastr.success com título customizado', () => {
    service.success('Salvo!', 'Perfil');
    expect(toastr.success).toHaveBeenCalledWith('Salvo!', 'Perfil');
  });

  it('deve chamar toastr.error com mensagem e título padrão', () => {
    service.error('Falhou!');
    expect(toastr.error).toHaveBeenCalledWith('Falhou!', 'Erro');
  });

  it('deve chamar toastr.info com mensagem e título padrão', () => {
    service.info('Informação');
    expect(toastr.info).toHaveBeenCalledWith('Informação', 'Info');
  });

  it('deve chamar toastr.warning com mensagem e título padrão', () => {
    service.warning('Atenção!');
    expect(toastr.warning).toHaveBeenCalledWith('Atenção!', 'Atenção');
  });
});