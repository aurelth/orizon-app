import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TrelloCardComponent } from './trello-card';

describe('TrelloCardComponent', () => {
  let component: TrelloCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
    }).compileComponents();

    component = TestBed.runInInjectionContext(() => new TrelloCardComponent());
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar tasks como array vazio', () => {
    expect(component.tasks()).toEqual([]);
  });

  it('deve retornar inProgressTasks vazio por padrão', () => {
    expect(component.inProgressTasks()).toEqual([]);
  });

  it('deve retornar todayTasks vazio por padrão', () => {
    expect(component.todayTasks()).toEqual([]);
  });
});