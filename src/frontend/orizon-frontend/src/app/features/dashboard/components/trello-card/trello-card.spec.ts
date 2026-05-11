import { TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { TrelloCardComponent } from './trello-card';
import { TrelloTask } from '../../../../core/briefing/models/briefing.model';

const makeTask = (i: number, type: 'today' | 'inprogress'): TrelloTask => ({
  cardId: `card-${i}`,
  title: `Task ${i}`,
  boardName: 'Board',
  boardColor: '#fff',
  listName: type === 'today' ? 'Today' : 'In Progress',
  columnType: type,
  movedToInProgressAt: null,
  daysInProgress: null,
  isStuck: false,
});

describe('TrelloCardComponent', () => {
  let component: TrelloCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
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

  it('deve filtrar tarefas inprogress corretamente', () => {
    const tasks = [
      makeTask(1, 'inprogress'),
      makeTask(2, 'today'),
      makeTask(3, 'inprogress'),
    ];
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    expect(component.inProgressTasks().length).toBe(2);
  });

  it('deve filtrar tarefas today corretamente', () => {
    const tasks = [
      makeTask(1, 'today'),
      makeTask(2, 'inprogress'),
      makeTask(3, 'today'),
    ];
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    expect(component.todayTasks().length).toBe(2);
  });

  it('deve mostrar no máximo 4 tarefas por padrão', () => {
    const tasks = [
      makeTask(1, 'inprogress'),
      makeTask(2, 'inprogress'),
      makeTask(3, 'inprogress'),
      makeTask(4, 'today'),
      makeTask(5, 'today'),
    ];
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    const total = component.visibleInProgress().length +
      component.visibleToday().length;
    expect(total).toBe(4);
  });

  it('deve mostrar botão Ver mais quando total > 4', () => {
    const tasks = [1, 2, 3, 4, 5].map(i => makeTask(i, 'today'));
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    expect(component.hasMore()).toBe(true);
  });

  it('não deve mostrar botão Ver mais quando total <= 4', () => {
    const tasks = [1, 2, 3, 4].map(i => makeTask(i, 'today'));
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    expect(component.hasMore()).toBe(false);
  });

  it('deve mostrar todas as tarefas ao chamar toggleShowAll', () => {
    const tasks = [1, 2, 3, 4, 5].map(i => makeTask(i, 'today'));
    TestBed.runInInjectionContext(() => {
      component = new TrelloCardComponent();
    });
    (component as any).tasks = () => tasks;

    component.toggleShowAll();
    expect(component.visibleToday().length).toBe(5);
  });
});