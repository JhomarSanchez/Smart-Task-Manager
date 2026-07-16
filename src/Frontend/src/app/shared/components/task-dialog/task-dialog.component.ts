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
  @Input() formErrors: { [key: string]: string[] | null } = {};

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<{
    title: string;
    description: string | null;
    dueDate: string | null;
    priority: string;
    category: string;
  }>();

  readonly modalTitle = signal<string>('');
  readonly modalDescription = signal<string>('');
  readonly modalDueDate = signal<string>('');
  readonly modalPriority = signal<string>('Medium');
  readonly modalCategory = signal<string>('General');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      if (this.task) {
        this.modalTitle.set(this.task.title);
        this.modalDescription.set(this.task.description || '');
        this.modalDueDate.set(this.task.dueDate ? this.task.dueDate.substring(0, 16) : '');
        this.modalPriority.set(this.task.priority);
        this.modalCategory.set(this.task.category || 'General');
      } else {
        this.modalTitle.set(this.initialTitle);
        this.modalDescription.set('');
        this.modalDueDate.set('');
        this.modalPriority.set('Medium');
        this.modalCategory.set(this.defaultCategory);
      }
    }
  }

  onCloseClick(): void {
    this.close.emit();
  }

  onSaveClick(): void {
    this.save.emit({
      title: this.modalTitle(),
      description: this.modalDescription() || null,
      dueDate: this.modalDueDate() || null,
      priority: this.modalPriority(),
      category: this.modalCategory() || 'General'
    });
  }
}
