import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskItem } from '../../core/models/task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-card.html',
  styleUrl: './task-card.css'
})
export class TaskCardComponent {
  @Input({ required: true }) task!: TaskItem;

  @Output() edit = new EventEmitter<TaskItem>();
  @Output() delete = new EventEmitter<string>();

  readonly showMenu = signal<boolean>(false);

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.showMenu.set(!this.showMenu());
  }

  closeMenu(): void {
    this.showMenu.set(false);
  }

  onEditClick(event: Event): void {
    event.stopPropagation();
    this.edit.emit(this.task);
    this.showMenu.set(false);
  }

  onDeleteClick(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.task.id);
    this.showMenu.set(false);
  }
}
