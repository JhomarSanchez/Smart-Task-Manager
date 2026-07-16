import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { TaskItem, CreateTaskDto, UpdateTaskDto, SmartParseRequestDto, SmartParseResponseDto } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5100/api/tasks';

  // State Signals
  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // Computed Columns (automatically sorted by board position)
  readonly todoTasks = computed(() => 
    this.tasks()
      .filter(t => t.status.toLowerCase() === 'todo')
      .sort((a, b) => a.boardPosition - b.boardPosition)
  );

  readonly doingTasks = computed(() => 
    this.tasks()
      .filter(t => t.status.toLowerCase() === 'doing' || t.status.toLowerCase() === 'inprogress')
      .sort((a, b) => a.boardPosition - b.boardPosition)
  );

  readonly doneTasks = computed(() => 
    this.tasks()
      .filter(t => t.status.toLowerCase() === 'done')
      .sort((a, b) => a.boardPosition - b.boardPosition)
  );

  /**
   * Loads all tasks from the API and updates the state.
   */
  async loadTasks(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const data = await firstValueFrom(this.http.get<TaskItem[]>(this.apiUrl));
      this.tasks.set(data);
    } catch (err: any) {
      this.error.set(err.message || 'Failed to load tasks from server.');
      console.error('Error loading tasks:', err);
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Creates a new task manually.
   */
  async createTask(dto: CreateTaskDto): Promise<TaskItem | null> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const newTask = await firstValueFrom(this.http.post<TaskItem>(this.apiUrl, dto));
      this.tasks.update(current => [...current, newTask]);
      return newTask;
    } catch (err: any) {
      this.error.set(err.error?.Message || err.message || 'Failed to create task.');
      console.error('Error creating task:', err);
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Updates task details.
   */
  async updateTask(id: string, dto: UpdateTaskDto): Promise<TaskItem | null> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const updatedTask = await firstValueFrom(this.http.put<TaskItem>(`${this.apiUrl}/${id}`, dto));
      this.tasks.update(current => 
        current.map(t => t.id === id ? updatedTask : t)
      );
      return updatedTask;
    } catch (err: any) {
      this.error.set(err.error?.Message || err.message || 'Failed to update task.');
      console.error('Error updating task:', err);
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Deletes a task by ID.
   */
  async deleteTask(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.http.delete(`${this.apiUrl}/${id}`));
      this.tasks.update(current => current.filter(t => t.id !== id));
    } catch (err: any) {
      this.error.set(err.error?.Message || err.message || 'Failed to delete task.');
      console.error('Error deleting task:', err);
      throw err;
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Reorders a task (optimistic update with fallback).
   */
  async reorderTask(id: string, newStatus: string, newPosition: number, newCategory?: string): Promise<void> {
    const originalTasks = [...this.tasks()];
    
    // Find task and update local state immediately (Optimistic Update)
    const taskIndex = originalTasks.findIndex(t => t.id === id);
    if (taskIndex === -1) return;

    const task = originalTasks[taskIndex];
    const targetCategory = newCategory !== undefined ? newCategory : task.category;
    
    // Create the payload matching UpdateTaskDto
    const updateDto: UpdateTaskDto = {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
      priority: task.priority,
      category: targetCategory,
      status: newStatus,
      boardPosition: newPosition
    };

    // Update signal state immediately
    this.tasks.update(current => 
      current.map(t => t.id === id ? { ...t, status: newStatus, boardPosition: newPosition, category: targetCategory, updatedAt: new Date().toISOString() } : t)
    );

    try {
      await firstValueFrom(this.http.put<TaskItem>(`${this.apiUrl}/${id}`, updateDto));
    } catch (err: any) {
      console.error('Failed to sync reorder with backend. Rolling back.', err);
      this.error.set('Failed to save task position on server. Reverted.');
      // Rollback to original state on failure
      this.tasks.set(originalTasks);
    }
  }

  /**
   * Retrieves user AI settings from localStorage.
   */
  getAiSettings() {
    if (typeof window !== 'undefined' && window.localStorage) {
      const saved = window.localStorage.getItem('smarttask_ai_settings');
      if (saved) {
        try {
          return JSON.parse(saved);
        } catch (e) {
          // ignore fallback to default
        }
      }
    }
    return {
      enabled: false,
      provider: 'Gemini',
      apiKey: '',
      modelName: 'gemini-1.5-flash'
    };
  }

  /**
   * Saves user AI settings to localStorage.
   */
  saveAiSettings(settings: { enabled: boolean; provider: string; apiKey: string; modelName: string }) {
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem('smarttask_ai_settings', JSON.stringify(settings));
    }
  }

  /**
   * Retrieves user-created boards (categories with no tasks yet) from localStorage.
   */
  getLocalBoards(): string[] {
    if (typeof window !== 'undefined' && window.localStorage) {
      const saved = window.localStorage.getItem('smarttask_local_boards');
      if (saved) {
        try {
          return JSON.parse(saved);
        } catch (e) {
          // ignore fallback to default
        }
      }
    }
    return [];
  }

  /**
   * Persists user-created boards to localStorage.
   */
  saveLocalBoards(boards: string[]): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem('smarttask_local_boards', JSON.stringify(boards));
    }
  }

  /**
   * Sends text to the AI smart-parse endpoint.
   */
  async smartParse(text: string): Promise<SmartParseResponseDto> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const payload: SmartParseRequestDto = { text };
      const settings = this.getAiSettings();
      const headers: { [key: string]: string } = {};

      if (settings.enabled && settings.apiKey?.trim()) {
        headers['X-AI-Enabled'] = 'true';
        headers['X-AI-Provider'] = settings.provider;
        headers['X-AI-ApiKey'] = settings.apiKey.trim();
        headers['X-AI-ModelName'] = settings.modelName?.trim() || 
          (settings.provider.toLowerCase() === 'openai' ? 'gpt-4o-mini' : 'gemini-1.5-flash');
      } else {
        headers['X-AI-Enabled'] = 'false';
      }

      return await firstValueFrom(
        this.http.post<SmartParseResponseDto>(
          `${this.apiUrl}/smart-parse`, 
          payload,
          { headers }
        )
      );
    } catch (err: any) {
      this.error.set(err.error?.Message || err.message || 'Failed to parse text via AI.');
      console.error('Error in smart parse:', err);
      
      // Local fallback in case of connection failure, mapping input text to Title
      return {
        isParsed: false,
        title: text,
        description: null,
        dueDate: null,
        priority: 'Medium',
        category: 'General'
      };
    } finally {
      this.loading.set(false);
    }
  }
}
