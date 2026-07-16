import { Component, EventEmitter, Input, Output, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskItem } from '../../../core/models/task.model';

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-dialog.component.html',
  styleUrl: './task-dialog.component.css'
})
export class TaskDialogComponent implements OnChanges {
  @Input({ required: true }) isOpen: boolean = false;
  @Input() task: TaskItem | null = null;
  @Input() initialTitle: string = '';
  @Input() defaultCategory: string = 'General';
  @Input() defaultColumn: string = 'To Do';
  @Input() columns: string[] = ['To Do', 'Doing', 'Done'];
  @Input() formErrors: { [key: string]: string[] | null } = {};
  @Input() saveError: string | null = null;

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<{
    title: string;
    description: string | null;
    dueDate: string | null;
    priority: string;
    category: string;
    columnName?: string;
  }>();

  readonly modalTitle = signal<string>('');
  readonly modalDescription = signal<string>('');
  // Separate date and time fields
  readonly modalDate = signal<string>('');
  readonly modalTime = signal<string>('');
  readonly modalPriority = signal<string>('Medium');
  readonly modalCategory = signal<string>('General');
  readonly modalColumn = signal<string>('To Do');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      if (this.task) {
        this.modalTitle.set(this.task.title);
        this.modalDescription.set(this.task.description || '');
        
        if (this.task.dueDate) {
          // Parse existing dueDate into separate date and time
          const dt = new Date(this.task.dueDate);
          const dateStr = dt.toISOString().substring(0, 10); // YYYY-MM-DD
          const hours = String(dt.getHours()).padStart(2, '0');
          const minutes = String(dt.getMinutes()).padStart(2, '0');
          this.modalDate.set(dateStr);
          this.modalTime.set(`${hours}:${minutes}`);
        } else {
          this.modalDate.set('');
          this.modalTime.set('');
        }

        this.modalPriority.set(this.task.priority);
        this.modalCategory.set(this.task.category || 'General');
        
        // Determine current column from category
        const parts = (this.task.category || '').split(':');
        const existingCol = parts[1]?.trim() || this.getDefaultColForStatus(this.task.status);
        this.modalColumn.set(existingCol);
      } else {
        this.modalTitle.set(this.initialTitle);
        this.modalDescription.set('');
        this.modalDate.set('');
        this.modalTime.set('');
        this.modalPriority.set('Medium');
        this.modalCategory.set(this.defaultCategory);
        this.modalColumn.set(this.defaultColumn || 'To Do');
      }
    }
  }

  private getDefaultColForStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'todo') return 'To Do';
    if (s === 'doing' || s === 'inprogress') return 'Doing';
    if (s === 'done') return 'Done';
    return 'To Do';
  }

  onCloseClick(): void {
    this.close.emit();
  }

  onSaveClick(): void {
    // Combine date and time into ISO string if both provided
    let dueDate: string | null = null;
    const dateVal = this.modalDate();
    if (dateVal) {
      const timeVal = this.modalTime() || '00:00';
      // Build a local datetime string and convert to ISO
      dueDate = new Date(`${dateVal}T${timeVal}`).toISOString();
    }

    this.save.emit({
      title: this.modalTitle(),
      description: this.modalDescription() || null,
      dueDate,
      priority: this.modalPriority(),
      category: this.modalCategory() || 'General',
      columnName: this.modalColumn()
    });
  }
}
