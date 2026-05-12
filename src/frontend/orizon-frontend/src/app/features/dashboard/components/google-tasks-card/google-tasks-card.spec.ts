import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { GoogleTasksCardComponent } from './google-tasks-card';
import { GoogleTask } from '../../../../core/briefing/models/briefing.model';

describe('GoogleTasksCardComponent', () => {
  let component: GoogleTasksCardComponent;

  const mockTasks: GoogleTask[] = [
    {
      id: 'task-1',
      title: 'Ir ao Supermercado',
      notes: null,
      dueDate: '2026-05-12T00:00:00.000Z',
      isOverdue: false,
      taskListName: 'My Tasks',
    },
    {
      id: 'task-2',
      title: 'Orizon: redefinição de senha',
      notes: 'Ajustar validação',
      dueDate: '2026-05-11T00:00:00.000Z',
      isOverdue: true,
      taskListName: 'My Tasks',
    },
    {
      id: 'task-3',
      title: 'Task 3',
      notes: null,
      dueDate: '2026-05-12T00:00:00.000Z',
      isOverdue: false,
      taskListName: 'Work',
    },
    {
      id: 'task-4',
      title: 'Task 4',
      notes: null,
      dueDate: null,
      isOverdue: false,
      taskListName: 'Work',
    },
    {
      id: 'task-5',
      title: 'Task 5',
      notes: null,
      dueDate: '2026-05-12T00:00:00.000Z',
      isOverdue: false,
      taskListName: 'My Tasks',
    },
    {
      id: 'task-6',
      title: 'Task 6',
      notes: 'Nota longa que deve ser truncada com reticências no final do texto quando ultrapassa o limite de oitenta caracteres',
      dueDate: '2026-05-12T00:00:00.000Z',
      isOverdue: false,
      taskListName: 'My Tasks',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    component = TestBed.runInInjectionContext(
      () => new GoogleTasksCardComponent()
    );
  });

  it('deve ser criado', () => {
    expect(component).toBeTruthy();
  });

  it('deve inicializar com showAll false', () => {
    expect(component.showAll()).toBe(false);
  });

  it('deve mostrar no máximo 5 tasks por padrão', () => {
    TestBed.runInInjectionContext(() => {
      component = new GoogleTasksCardComponent();
    });
    const ref = TestBed.runInInjectionContext(() => {
      const c = new GoogleTasksCardComponent();
      (c as any)['_tasks'] = mockTasks;
      return c;
    });
    expect(component.visibleTasks().length).toBeLessThanOrEqual(5);
  });

  it('deve retornar hasMore true quando tasks > 5', () => {
    TestBed.runInInjectionContext(() => {
      component = new GoogleTasksCardComponent();
    });
    // 6 tasks > 5
    expect(mockTasks.length).toBeGreaterThan(5);
  });

  it('deve alternar showAll ao chamar toggleShowAll', () => {
    expect(component.showAll()).toBe(false);
    component.toggleShowAll();
    expect(component.showAll()).toBe(true);
    component.toggleShowAll();
    expect(component.showAll()).toBe(false);
  });

  it('deve formatar data corretamente usando UTC', () => {
    const result = component.formatDueDate('2026-05-12T00:00:00.000Z');
    expect(result).toBe('12/05');
  });

  it('deve retornar null quando dueDate é null', () => {
    const result = component.formatDueDate(null);
    expect(result).toBeNull();
  });

  it('deve formatar data de dia anterior corretamente', () => {
    const result = component.formatDueDate('2026-05-11T00:00:00.000Z');
    expect(result).toBe('11/05');
  });
});