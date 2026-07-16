import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskItem } from '../../core/models/task.model';

export type TaskCardVariant = 'todo' | 'doing' | 'done';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-card.html',
  styleUrl: './task-card.css'
})
export class TaskCardComponent {
  @Input({ required: true }) task!: TaskItem;
  @Input() variant: TaskCardVariant = 'todo';

  @Output() edit = new EventEmitter<TaskItem>();
  @Output() delete = new EventEmitter<string>();

  onCardClick(): void {
    this.edit.emit(this.task);
  }

  onDeleteClick(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.task.id);
  }
}
