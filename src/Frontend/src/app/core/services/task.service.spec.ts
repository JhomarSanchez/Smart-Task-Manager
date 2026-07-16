import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TaskService } from './task.service';
import { TaskItem, CreateTaskDto, UpdateTaskDto } from '../models/task.model';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TaskService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with an empty tasks list', () => {
    expect(service.tasks()).toEqual([]);
  });

  it('should correctly filter tasks into computed columns', () => {
    const dummyTasks: TaskItem[] = [
      { id: '1', title: 'Task 1', description: null, dueDate: null, priority: 'Medium', category: 'General', status: 'Todo', boardPosition: 2000, createdAt: '', updatedAt: '' },
      { id: '2', title: 'Task 2', description: null, dueDate: null, priority: 'High', category: 'General', status: 'Doing', boardPosition: 1000, createdAt: '', updatedAt: '' },
      { id: '3', title: 'Task 3', description: null, dueDate: null, priority: 'Low', category: 'General', status: 'Done', boardPosition: 1500, createdAt: '', updatedAt: '' },
      { id: '4', title: 'Task 4', description: null, dueDate: null, priority: 'Medium', category: 'General', status: 'Todo', boardPosition: 1000, createdAt: '', updatedAt: '' }
    ];

    service.tasks.set(dummyTasks);

    const todo = service.todoTasks();
    const doing = service.doingTasks();
    const done = service.doneTasks();

    expect(todo.length).toBe(2);
    expect(todo[0].id).toBe('4'); // sorted by position 1000 before 2000
    expect(todo[1].id).toBe('1');

    expect(doing.length).toBe(1);
    expect(doing[0].id).toBe('2');

    expect(done.length).toBe(1);
    expect(done[0].id).toBe('3');
  });

  it('should optimistically update tasks list on reorderTask', async () => {
    const dummyTasks: TaskItem[] = [
      { id: '1', title: 'Task 1', description: null, dueDate: null, priority: 'Medium', category: 'General', status: 'Todo', boardPosition: 1000, createdAt: '', updatedAt: '' }
    ];
    service.tasks.set(dummyTasks);

    // Call reorderTask (Todo -> Doing, position 500)
    const reorderPromise = service.reorderTask('1', 'Doing', 500);

    // State should immediately be updated (Optimistic update)
    const todo = service.todoTasks();
    const doing = service.doingTasks();
    expect(todo.length).toBe(0);
    expect(doing.length).toBe(1);
    expect(doing[0].boardPosition).toBe(500);
    expect(doing[0].status).toBe('Doing');

    // Mock API response
    const req = httpMock.expectOne('http://localhost:5100/api/tasks/1');
    expect(req.request.method).toBe('PUT');
    req.flush({ ...dummyTasks[0], status: 'Doing', boardPosition: 500 });

    await reorderPromise;
  });

  it('should rollback tasks list on reorderTask failure', async () => {
    const dummyTasks: TaskItem[] = [
      { id: '1', title: 'Task 1', description: null, dueDate: null, priority: 'Medium', category: 'General', status: 'Todo', boardPosition: 1000, createdAt: '', updatedAt: '' }
    ];
    service.tasks.set(dummyTasks);

    // Call reorderTask
    const reorderPromise = service.reorderTask('1', 'Doing', 500);

    // Mock API error
    const req = httpMock.expectOne('http://localhost:5100/api/tasks/1');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

    await reorderPromise;

    // State should have rolled back to original
    expect(service.todoTasks().length).toBe(1);
    expect(service.doingTasks().length).toBe(0);
    expect(service.todoTasks()[0].status).toBe('Todo');
  });
});
