export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: string;
  category: string;
  status: string;
  boardPosition: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskDto {
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: string;
  category: string;
}

export interface UpdateTaskDto {
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: string;
  category: string;
  status: string;
  boardPosition: number;
}

export interface SmartParseRequestDto {
  text: string;
}

export interface SmartParseResponseDto {
  isParsed: boolean;
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: string;
  category: string;
}
