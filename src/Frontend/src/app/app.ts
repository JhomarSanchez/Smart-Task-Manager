import { Component } from '@angular/core';
import { KanbanBoardComponent } from './features/kanban/kanban-board.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [KanbanBoardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
}
