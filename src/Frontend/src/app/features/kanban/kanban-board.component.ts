import { Component, inject, signal, computed, effect, OnInit, HostListener, ViewChildren, QueryList } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, CreateTaskDto, UpdateTaskDto } from '../../core/models/task.model';
import { TaskCardComponent } from '../../shared/task-card/task-card';
import { TaskDialogComponent } from '../../shared/components/task-dialog/task-dialog.component';
import { SettingsModalComponent } from '../settings/settings-modal.component';

type ThemeMode = 'light' | 'dark' | 'system';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    DragDropModule,
    TaskCardComponent,
    TaskDialogComponent,
    SettingsModalComponent
  ],
  templateUrl: './kanban-board.component.html',
  styleUrl: './kanban-board.component.css'
})
export class KanbanBoardComponent implements OnInit {
  protected readonly taskService = inject(TaskService);

  @ViewChildren(TaskCardComponent) cardComponents!: QueryList<TaskCardComponent>;

  // UI state
  readonly themeMode = signal<ThemeMode>('system');
  readonly resolvedTheme = signal<'light' | 'dark'>('dark');
  readonly rawInput = signal<string>('');
  readonly isParsing = signal<boolean>(false);
  readonly showModal = signal<boolean>(false);
  readonly showSettingsModal = signal<boolean>(false);
  readonly editingTask = signal<TaskItem | null>(null);
  readonly saveError = signal<string | null>(null);

  // Dropdown states
  readonly showBoardDropdown = signal<boolean>(false);
  readonly showThemeDropdown = signal<boolean>(false);

  // Boards (Categories) State
  readonly activeBoard = signal<string>('General');
  readonly localBoards = signal<string[]>([]);
  readonly boards = computed(() => {
    const cats = new Set<string>();
    this.taskService.tasks().forEach(t => {
      if (t.category?.trim()) {
        const boardName = t.category.split(':')[0].trim();
        if (boardName) cats.add(boardName);
      }
    });
    this.localBoards().forEach(b => cats.add(b));
    cats.add('General');
    return Array.from(cats).sort();
  });

  // Dynamic Column State
  readonly localColumns = signal<{ [board: string]: string[] }>({});
  
  readonly columnsForActiveBoard = computed(() => {
    const board = this.activeBoard();
    // Maintain insertion order: default columns first, then custom ones
    const defaultCols = ['To Do', 'Doing', 'Done'];
    const customCols: string[] = [];
    
    this.taskService.tasks().forEach(t => {
      const cat = t.category || 'General';
      const parts = cat.split(':');
      if (parts[0].trim().toLowerCase() === board.toLowerCase() && parts[1]?.trim()) {
        const colName = parts[1].trim();
        if (!defaultCols.includes(colName) && !customCols.includes(colName)) {
          customCols.push(colName);
        }
      }
    });
    
    const local = this.localColumns()[board] || [];
    local.forEach(c => {
      if (!defaultCols.includes(c) && !customCols.includes(c)) {
        customCols.push(c);
      }
    });
    
    return [...defaultCols, ...customCols];
  });

  // Form values (Task Create/Edit)
  readonly modalTitle = signal<string>('');
  readonly modalDefaultColumn = signal<string>('To Do');
  readonly formErrors = signal<{ [key: string]: string[] | null }>({});

  // AI Configuration values
  readonly aiEnabled = signal<boolean>(false);
  readonly aiProvider = signal<string>('Gemini');
  readonly aiApiKey = signal<string>('');
  readonly aiModelName = signal<string>('gemini-1.5-flash');

  // System theme media query listener
  private systemThemeMedia: MediaQueryList | null = null;

  constructor() {
    effect(() => {
      const root = document.documentElement;
      const resolved = this.resolvedTheme();
      if (resolved === 'dark') {
        root.classList.add('dark');
      } else {
        root.classList.remove('dark');
      }
    });
  }

  ngOnInit(): void {
    this.taskService.loadTasks();

    // Load AI settings from localStorage
    const ai = this.taskService.getAiSettings();
    this.aiEnabled.set(ai.enabled);
    this.aiProvider.set(ai.provider);
    this.aiApiKey.set(ai.apiKey);
    this.aiModelName.set(ai.modelName);

    // Load local boards from localStorage
    const savedBoards = this.taskService.getLocalBoards();
    if (savedBoards.length > 0) {
      this.localBoards.set(savedBoards);
    }

    // Load local columns from localStorage
    this.loadLocalColumnsFromStorage();

    // Initialize theme: read from localStorage, then fall back to system preference
    this.initTheme();
  }

  private initTheme(): void {
    if (typeof window === 'undefined') return;

    // Set up system theme media query listener (guard for jsdom/test environments)
    if (typeof window.matchMedia === 'function') {
      this.systemThemeMedia = window.matchMedia('(prefers-color-scheme: dark)');
      this.systemThemeMedia.addEventListener('change', () => {
        if (this.themeMode() === 'system') {
          this.resolvedTheme.set(this.systemThemeMedia!.matches ? 'dark' : 'light');
        }
      });
    }
    
    // Read saved theme preference
    const savedMode = window.localStorage?.getItem('smarttask_theme') as ThemeMode | null;
    const mode = savedMode ?? 'system';
    
    this.themeMode.set(mode);
    this.applyThemeMode(mode);
  }

  private applyThemeMode(mode: ThemeMode): void {
    if (mode === 'system') {
      const systemDark = this.systemThemeMedia?.matches ?? false;
      this.resolvedTheme.set(systemDark ? 'dark' : 'light');
    } else {
      this.resolvedTheme.set(mode);
    }
  }

  setThemeMode(mode: ThemeMode): void {
    this.themeMode.set(mode);
    this.applyThemeMode(mode);
    if (typeof window !== 'undefined') {
      window.localStorage.setItem('smarttask_theme', mode);
    }
    this.showThemeDropdown.set(false);
  }

  private loadLocalColumnsFromStorage(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      const saved = window.localStorage.getItem('smarttask_local_columns');
      if (saved) {
        try {
          this.localColumns.set(JSON.parse(saved));
        } catch { /* ignore */ }
      }
    }
  }

  private saveLocalColumnsToStorage(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem('smarttask_local_columns', JSON.stringify(this.localColumns()));
    }
  }

  // Convert column name to a valid HTML ID (no spaces, lowercase)
  getColumnListId(columnName: string): string {
    return 'col-' + columnName.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '');
  }

  // Intercept global clicks to close menus
  @HostListener('document:click')
  closeDropdowns(): void {
    this.showBoardDropdown.set(false);
    this.showThemeDropdown.set(false);
    this.cardComponents?.forEach(c => c.closeMenu());
  }

  toggleBoardDropdown(event: Event): void {
    event.stopPropagation();
    this.showThemeDropdown.set(false);
    this.showBoardDropdown.set(!this.showBoardDropdown());
  }

  toggleThemeDropdown(event: Event): void {
    event.stopPropagation();
    this.showBoardDropdown.set(false);
    this.showThemeDropdown.set(!this.showThemeDropdown());
  }

  // Board management
  createNewBoard(): void {
    const name = prompt('Escribe el nombre del nuevo tablero:');
    if (name && name.trim()) {
      const trimmed = name.trim();
      if (!this.localBoards().includes(trimmed)) {
        const updated = [...this.localBoards(), trimmed];
        this.localBoards.set(updated);
        this.taskService.saveLocalBoards(updated);
      }
      this.activeBoard.set(trimmed);
      this.showBoardDropdown.set(false);
    }
  }

  selectBoard(board: string): void {
    this.activeBoard.set(board);
    this.showBoardDropdown.set(false);
  }

  // Column management
  createNewColumn(): void {
    const name = prompt('Escribe el nombre de la nueva columna:');
    if (name && name.trim()) {
      const trimmed = name.trim();
      const currentCols = this.columnsForActiveBoard();
      
      if (currentCols.some(c => c.toLowerCase() === trimmed.toLowerCase())) {
        alert('Esa columna ya existe en este tablero.');
        return;
      }
      
      const board = this.activeBoard();
      this.localColumns.update(curr => {
        const list = curr[board] || [];
        return { ...curr, [board]: [...list, trimmed] };
      });
      this.saveLocalColumnsToStorage();
    }
  }

  deleteColumn(columnName: string): void {
    if (['To Do', 'Doing', 'Done'].includes(columnName)) return;

    if (confirm(`¿Eliminar la columna "${columnName}"? Las tareas se moverán a "To Do".`)) {
      const board = this.activeBoard();
      const tasksInCol = this.getTasksForColumn(columnName);

      // Move tasks to To Do asynchronously
      Promise.all(tasksInCol.map(t => {
        const dto: UpdateTaskDto = {
          title: t.title,
          description: t.description,
          dueDate: t.dueDate,
          priority: t.priority,
          category: board,
          status: 'Todo',
          boardPosition: t.boardPosition
        };
        return this.taskService.updateTask(t.id, dto).catch(err => {
          console.error('Error shifting task column:', err);
        });
      }));

      this.localColumns.update(curr => {
        const list = curr[board] || [];
        return { ...curr, [board]: list.filter(c => c !== columnName) };
      });
      this.saveLocalColumnsToStorage();
    }
  }

  getTasksForColumn(columnName: string): TaskItem[] {
    const board = this.activeBoard();
    return this.taskService.tasks()
      .filter(t => {
        const cat = t.category || 'General';
        const parts = cat.split(':');
        const taskBoard = parts[0].trim();
        const taskCol = parts[1]?.trim() || this.getDefaultColumnForStatus(t.status);
        
        return taskBoard.toLowerCase() === board.toLowerCase() && 
               taskCol.toLowerCase() === columnName.toLowerCase();
      })
      .sort((a, b) => a.boardPosition - b.boardPosition);
  }

  private getDefaultColumnForStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'todo') return 'To Do';
    if (s === 'doing' || s === 'inprogress') return 'Doing';
    if (s === 'done') return 'Done';
    return 'To Do';
  }

  getConnectedLists(excludeColumn: string): string[] {
    return this.columnsForActiveBoard()
      .filter(c => c !== excludeColumn)
      .map(c => this.getColumnListId(c));
  }

  // AI settings
  openSettingsModal(): void {
    this.showSettingsModal.set(true);
  }

  closeSettingsModal(): void {
    this.showSettingsModal.set(false);
  }

  saveAiSettings(settings: { enabled: boolean; provider: string; apiKey: string; modelName: string }): void {
    this.taskService.saveAiSettings(settings);
    this.aiEnabled.set(settings.enabled);
    this.aiProvider.set(settings.provider);
    this.aiApiKey.set(settings.apiKey);
    this.aiModelName.set(settings.modelName);
    this.showSettingsModal.set(false);
  }

  // Task dialog
  openCreateModal(initialTitle: string = '', columnName: string = 'To Do'): void {
    this.editingTask.set(null);
    this.modalTitle.set(initialTitle);
    this.modalDefaultColumn.set(columnName);
    this.formErrors.set({});
    this.saveError.set(null);
    this.showModal.set(true);
  }

  openEditModal(task: TaskItem): void {
    this.editingTask.set(task);
    this.formErrors.set({});
    this.saveError.set(null);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.saveError.set(null);
  }

  async saveTask(payload: {
    title: string;
    description: string | null;
    dueDate: string | null;
    priority: string;
    category: string;
    columnName?: string;
  }): Promise<void> {
    this.formErrors.set({});
    this.saveError.set(null);

    try {
      const task = this.editingTask();
      
      // Build the category with board prefix if a column name is specified
      let targetCategory = payload.category.trim() || this.activeBoard();
      const columnName = payload.columnName;

      // If the user picked a column, format the category as Board:Column
      // (only for custom columns, not default To Do/Doing/Done which just use board name)
      if (columnName && !['To Do', 'Doing', 'Done'].includes(columnName)) {
        const boardPart = targetCategory.split(':')[0].trim();
        targetCategory = `${boardPart}:${columnName}`;
      }

      // Determine status from column or category
      const colForStatus = columnName || targetCategory.split(':')[1]?.trim() || '';
      let targetStatus = 'Todo';
      if (colForStatus) {
        const colLower = colForStatus.toLowerCase();
        if (colLower === 'doing') targetStatus = 'Doing';
        else if (colLower === 'done') targetStatus = 'Done';
        else if (colLower === 'to do' || colLower === 'todo') targetStatus = 'Todo';
        else targetStatus = 'Doing'; // custom column -> Doing
      }

      if (task) {
        const dto: UpdateTaskDto = {
          title: payload.title,
          description: payload.description,
          dueDate: payload.dueDate,
          priority: payload.priority,
          category: targetCategory,
          status: targetStatus,
          boardPosition: task.boardPosition
        };
        await this.taskService.updateTask(task.id, dto);
      } else {
        const dto: CreateTaskDto = {
          title: payload.title,
          description: payload.description,
          dueDate: payload.dueDate,
          priority: payload.priority,
          category: targetCategory
        };
        const created = await this.taskService.createTask(dto);
        if (created && created.category) {
          const mainBoard = created.category.split(':')[0];
          this.activeBoard.set(mainBoard);
        }
      }
      this.showModal.set(false);
    } catch (err: any) {
      if (err.error?.Errors) {
        this.formErrors.set(err.error.Errors);
      } else if (err.error?.Message) {
        this.saveError.set(err.error.Message);
      } else {
        this.saveError.set('Error al guardar la tarea. Por favor intenta de nuevo.');
        console.error('Save error:', err);
      }
    }
  }

  async deleteTask(id: string): Promise<void> {
    if (confirm('¿Estás seguro de eliminar esta tarea?')) {
      try {
        await this.taskService.deleteTask(id);
      } catch (err) {
        console.error('Delete error:', err);
      }
    }
  }

  async handleSmartInput(): Promise<void> {
    const text = this.rawInput().trim();
    if (!text) return;

    this.isParsing.set(true);
    try {
      const parsed = await this.taskService.smartParse(text);
      if (parsed.isParsed) {
        const dto: CreateTaskDto = {
          title: parsed.title,
          description: parsed.description,
          dueDate: parsed.dueDate,
          priority: parsed.priority,
          category: parsed.category || this.activeBoard()
        };
        const created = await this.taskService.createTask(dto);
        if (created && created.category) {
          const mainBoard = created.category.split(':')[0];
          this.activeBoard.set(mainBoard);
        }
        this.rawInput.set('');
      } else {
        this.openCreateModal(parsed.title);
        this.rawInput.set('');
      }
    } catch (err) {
      console.error('Smart parse fail:', err);
      this.openCreateModal(text);
      this.rawInput.set('');
    } finally {
      this.isParsing.set(false);
    }
  }

  onCardDropped(event: CdkDragDrop<TaskItem[]>, targetColumnName: string): void {
    if (event.previousContainer === event.container) {
      // Reorder within same column
      const colTasks = [...this.getTasksForColumn(targetColumnName)];
      moveItemInArray(colTasks, event.previousIndex, event.currentIndex);
      
      const id = colTasks[event.currentIndex].id;
      const newPosition = this.calculatePosition(colTasks, event.currentIndex);
      const board = this.activeBoard();
      const newCategory = ['To Do', 'Doing', 'Done'].includes(targetColumnName)
        ? board
        : `${board}:${targetColumnName}`;
      const newStatus = this.getStatusForColumn(targetColumnName);
      
      this.taskService.reorderTask(id, newStatus, newPosition, newCategory);
    } else {
      // Move to a different column
      const item: TaskItem = event.item.data;
      if (!item) return;

      const targetTasks = [...this.getTasksForColumn(targetColumnName)];
      targetTasks.splice(event.currentIndex, 0, item);
      
      const newPosition = this.calculatePosition(targetTasks, event.currentIndex);
      const board = this.activeBoard();
      const newCategory = ['To Do', 'Doing', 'Done'].includes(targetColumnName)
        ? board
        : `${board}:${targetColumnName}`;
      const newStatus = this.getStatusForColumn(targetColumnName);
      
      this.taskService.reorderTask(item.id, newStatus, newPosition, newCategory);
    }
  }

  private getStatusForColumn(columnName: string): string {
    const col = columnName.toLowerCase();
    if (col === 'to do') return 'Todo';
    if (col === 'doing') return 'Doing';
    if (col === 'done') return 'Done';
    return 'Doing'; // custom columns map to Doing
  }

  private calculatePosition(list: TaskItem[], index: number): number {
    if (list.length <= 1) return 1000;
    if (index === 0) return Math.max(1, (list[1]?.boardPosition ?? 2000) / 2);
    if (index === list.length - 1) return (list[index - 1]?.boardPosition ?? 0) + 1000;
    const prev = list[index - 1]?.boardPosition ?? 0;
    const next = list[index + 1]?.boardPosition ?? prev + 2000;
    return (prev + next) / 2;
  }
}
