import { TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { EmailsCardComponent } from './emails-card';
import { EmailSummary } from '../../../../core/briefing/models/briefing.model';

const makeEmail = (i: number): EmailSummary => ({
  from: `sender${i}@test.com`,
  subject: `Subject ${i}`,
  aiSummary: `Summary ${i}`,
  category: 'Info',
  categoryEmoji: '📧',
  receivedAt: '2026-05-10T10:00:00Z',
});

describe('EmailsCardComponent', () => {
  let component: EmailsCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new EmailsCardComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar com lista vazia', () => {
    expect(component.emails()).toEqual([]);
  });

  it('deve mostrar apenas 3 emails por padrão', () => {
    const emails = [1, 2, 3, 4, 5].map(makeEmail);
    TestBed.runInInjectionContext(() => {
      component = new EmailsCardComponent();
    });
    (component as any).emails = () => emails;

    expect(component.visibleEmails().length).toBe(3);
  });

  it('não deve mostrar botão Ver mais quando há 3 ou menos emails', () => {
    const emails = [1, 2, 3].map(makeEmail);
    TestBed.runInInjectionContext(() => {
      component = new EmailsCardComponent();
    });
    (component as any).emails = () => emails;

    expect(component.hasMore()).toBe(false);
  });

  it('deve mostrar botão Ver mais quando há mais de 3 emails', () => {
    const emails = [1, 2, 3, 4].map(makeEmail);
    TestBed.runInInjectionContext(() => {
      component = new EmailsCardComponent();
    });
    (component as any).emails = () => emails;

    expect(component.hasMore()).toBe(true);
  });

  it('deve mostrar todos os emails ao chamar toggleShowAll', () => {
    const emails = [1, 2, 3, 4, 5].map(makeEmail);
    TestBed.runInInjectionContext(() => {
      component = new EmailsCardComponent();
    });
    (component as any).emails = () => emails;

    component.toggleShowAll();
    expect(component.visibleEmails().length).toBe(5);
  });

  it('deve voltar para 3 emails ao chamar toggleShowAll novamente', () => {
    const emails = [1, 2, 3, 4, 5].map(makeEmail);
    TestBed.runInInjectionContext(() => {
      component = new EmailsCardComponent();
    });
    (component as any).emails = () => emails;

    component.toggleShowAll();
    component.toggleShowAll();
    expect(component.visibleEmails().length).toBe(3);
  });

  it('showAll deve inicializar como false', () => {
    expect(component.showAll()).toBe(false);
  });
});