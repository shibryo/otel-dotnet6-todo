export interface TodoItem {
    id: number;
    title: string;
    isComplete: boolean;
    createdAt: string;
    completedAt: string | null;
}

export interface CreateTodoItem {
    title: string;
}
