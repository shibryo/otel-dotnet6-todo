import axios from 'axios';
import { TodoItem, CreateTodoItem } from '../types/todo';

const api = axios.create({
    baseURL: '/api'  // nginxがプロキシするためルートパスから指定
});

export const todoApi = {
    getAll: async (): Promise<TodoItem[]> => {
        const response = await api.get<TodoItem[]>('/todoitems');
        return response.data;
    },

    create: async (todo: CreateTodoItem): Promise<TodoItem> => {
        const response = await api.post<TodoItem>('/todoitems', todo);
        return response.data;
    },

    update: async (id: number, todo: TodoItem): Promise<TodoItem> => {
        const response = await api.put<TodoItem>(`/todoitems/${id}`, todo);
        return response.data;
    },

    delete: async (id: number): Promise<void> => {
        await api.delete(`/todoitems/${id}`);
    },

    toggleComplete: async (id: number, todo: TodoItem): Promise<TodoItem> => {
        const updatedTodo = {
            ...todo,
            isComplete: !todo.isComplete,
            completedAt: !todo.isComplete ? new Date().toISOString() : null
        };
        return await todoApi.update(id, updatedTodo);
    }
};
