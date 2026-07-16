import { Component, inject, signal, computed, effect, OnInit, HostListener, ViewChildren, QueryList } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, CreateTaskDto, UpdateTaskDto } from '../../core/models/task.model';
import { TaskCardComponent } from '../../shared/task-card/task-card';
import { TaskDialogComponent } from '../../shared/components/task-dialog/task-dialog.component';
import { SettingsModalComponent } from '../settings/settings-modal.component';

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
  readonly theme = signal<'light' | 'dark'>('dark');
  readonly rawInput = signal<string>('');
  readonly isParsing = signal<boolean>(false);
  readonly showModal = signal<boolean>(false);
  readonly showSettingsModal = signal<boolean>(false);
  readonly editingTask = signal<TaskItem | null>(null);

  // Dropdown states
  readonly showBoardDropdown = signal<boolean>(false);

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
    const cols = new Set<string>(['To Do', 'Doing', 'Done']);
    
    this.taskService.tasks().forEach(t => {
      const cat = t.category || 'General';
      const parts = cat.split(':');
      if (parts[0].toLowerCase() === board.toLowerCase() && parts[1]?.trim()) {
        cols.add(parts[1].trim());
      }
    });
    
    const local = this.localColumns()[board] || [];
    local.forEach(c => cols.add(c));
    
    return Array.from(cols);
  });

  // Form values (Task Create/Edit)
  readonly modalTitle = signal<string>('');
  readonly formErrors = signal<{ [key: string]: string[] | null }>({});

  // AI Configuration values
  readonly aiEnabled = signal<boolean>(false);
  readonly aiProvider = signal<string>('Gemini');
  readonly aiApiKey = signal<string>('');
  readonly aiModelName = signal<string>('gemini-1.5-flash');

  constructor() {
    effect(() => {
      const root = document.documentElement;
      if (this.theme() === 'dark') {
        root.classList.add('dark');
      } else {
        root.classList.remove('dark');
      }
    });
  }

  ngOnInit(): void {
    this.taskService.loadTasks();

    const ai = this.taskService.getAiSettings();
    this.aiEnabled.set(ai.enabled);
    this.aiProvider.set(ai.provider);
    this.aiApiKey.set(ai.apiKey);
    this.aiModelName.set(ai.modelName);
  }

  toggleTheme(): void {
    this.theme.set(this.theme() === 'light' ? 'dark' : 'light');
  }

  // Intercept global clicks to close menus
  @HostListener('document:click')
  closeDropdowns(): void {
    this.showBoardDropdown.set(false);
    this.cardComponents?.forEach(c => c.closeMenu());
  }

  toggleBoardDropdown(event: Event): void {
    event.stopPropagation();
    this.showBoardDropdown.set(!this.showBoardDropdown());
  }

  // Board management
  createNewBoard(): void {
    const name = prompt('Escribe el nombre del nuevo tablero:');
    if (name && name.trim()) {
      const trimmed = name.trim();
      if (!this.localBoards().includes(trimmed)) {
        this.localBoards.update(curr => [...curr, trimmed]);
      }
      this.activeBoard.set(trimmed);
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
        return {
          ...curr,
          [board]: [...list, trimmed]
        };
      });
    }
  }

  deleteColumn(columnName: string): void {
    if (['To Do', 'Doing', 'Done'].includes(columnName)) return;

    if (confirm(`¿Estás seguro de eliminar la columna "${columnName}"? Las tareas asociadas se moverán a la columna "To Do".`)) {
      const board = this.activeBoard();
      const tasksInCol = this.getTasksForColumn(columnName);

      tasksInCol.forEach(async (t) => {
        const dto: UpdateTaskDto = {
          title: t.title,
          description: t.description,
          dueDate: t.dueDate,
          priority: t.priority,
          category: board, 
          status: 'Todo',
          boardPosition: t.boardPosition
        };
        try {
          await this.taskService.updateTask(t.id, dto);
        } catch (err) {
          console.error('Error shifting task column:', err);
        }
      });

      this.localColumns.update(curr => {
        const list = curr[board] || [];
        return {
          ...curr,
          [board]: list.filter(c => c !== columnName)
        };
      });
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
    const s = status ? status.toLowerCase() : 'todo';
    if (s === 'todo') return 'To Do';
    if (s === 'doing' || s === 'inprogress') return 'Doing';
    return 'Done';
  }

  getConnectedLists(excludeColumn: string): string[] {
    return this.columnsForActiveBoard()
      .filter(c => c !== excludeColumn)
      .map(c => c + 'List');
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
    this.formErrors.set({});
    this.showModal.set(true);
  }

  openEditModal(task: TaskItem): void {
    this.editingTask.set(task);
    this.formErrors.set({});
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  async saveTask(payload: {
    title: string;
    description: string | null;
    dueDate: string | null;
    priority: string;
    category: string;
  }): Promise<void> {
    this.formErrors.set({});

    try {
      const task = this.editingTask();
      const targetCategory = payload.category.trim() || 'General';
      
      let targetStatus = 'Todo';
      const parts = targetCategory.split(':');
      const colName = parts[1]?.trim() || '';
      if (colName) {
        if (colName.toLowerCase() === 'doing') targetStatus = 'Doing';
        else if (colName.toLowerCase() === 'done') targetStatus = 'Done';
        else if (colName.toLowerCase() === 'to do') targetStatus = 'Todo';
        else targetStatus = 'Doing';
      } else {
        targetStatus = 'Todo';
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
      } else {
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
      }
    } catch (err) {
      console.error('Smart parse fail:', err);
      this.openCreateModal(text);
    } finally {
      this.isParsing.set(false);
    }
  }

  onCardDropped(event: CdkDragDrop<TaskItem[]>, targetColumnName: string): void {
    const id = event.item.data.id;
    const isSameContainer = event.previousContainer === event.container;
    
    const targetListCopy = [...event.container.data];
    const item = event.previousContainer.data[event.previousIndex];

    if (isSameContainer) {
      targetListCopy.splice(event.previousIndex, 1);
      targetListCopy.splice(event.currentIndex, 0, item);
    } else {
      targetListCopy.splice(event.currentIndex, 0, item);
    }

    const newPosition = this.calculatePosition(targetListCopy, event.currentIndex);

    let newStatus = 'Todo';
    if (targetColumnName === 'To Do') {
      newStatus = 'Todo';
    } else if (targetColumnName === 'Doing') {
      newStatus = 'Doing';
    } else if (targetColumnName === 'Done') {
      newStatus = 'Done';
    } else {
      newStatus = 'Doing';
    }

    const board = this.activeBoard();
    const newCategory = ['To Do', 'Doing', 'Done'].includes(targetColumnName) 
      ? board 
      : `${board}:${targetColumnName}`;

    this.taskService.reorderTask(id, newStatus, newPosition, newCategory);
  }

  private calculatePosition(list: TaskItem[], index: number): number {
    if (list.length <= 1) {
      return 1000;
    }
    if (index === 0) {
      return list[1].boardPosition / 2;
    }
    if (index === list.length - 1) {
      return list[index - 1].boardPosition + 1000;
    }
    const prev = list[index - 1].boardPosition;
    const next = list[index + 1].boardPosition;
    return (prev + next) / 2;
  }
}
