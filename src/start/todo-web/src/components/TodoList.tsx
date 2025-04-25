import { useEffect, useState } from 'react';
import {
    List,
    ListItem,
    ListItemText,
    ListItemSecondaryAction,
    IconButton,
    Checkbox,
    Paper,
    Typography
} from '@mui/material';
import { Delete as DeleteIcon } from '@mui/icons-material';
import { TodoItem } from '../types/todo';
import { todoApi } from '../api/todoApi';

export const TodoList: React.FC = () => {
    const [todos, setTodos] = useState<TodoItem[]>([]);

    const loadTodos = async () => {
        try {
            const items = await todoApi.getAll();
            setTodos(items);
        } catch (error) {
            console.error('Failed to load todos:', error);
        }
    };

    useEffect(() => {
        loadTodos();
    }, []);

    const handleToggleComplete = async (todo: TodoItem) => {
        try {
            await todoApi.toggleComplete(todo.id, todo);
            loadTodos();
        } catch (error) {
            console.error('Failed to toggle todo:', error);
        }
    };

    const handleDelete = async (id: number) => {
        try {
            await todoApi.delete(id);
            loadTodos();
        } catch (error) {
            console.error('Failed to delete todo:', error);
        }
    };

    return (
        <Paper elevation={2} style={{ padding: '16px', marginTop: '16px' }}>
            <Typography variant="h6" component="h2" gutterBottom>
                Todo List
            </Typography>
            <List>
                {todos.map((todo) => (
                    <ListItem key={todo.id} divider>
                        <Checkbox
                            checked={todo.isComplete}
                            onChange={() => handleToggleComplete(todo)}
                        />
                        <ListItemText
                            primary={todo.title}
                            secondary={`作成日: ${new Date(todo.createdAt).toLocaleString()}`}
                            style={{
                                textDecoration: todo.isComplete ? 'line-through' : 'none'
                            }}
                        />
                        <ListItemSecondaryAction>
                            <IconButton
                                edge="end"
                                aria-label="delete"
                                onClick={() => handleDelete(todo.id)}
                            >
                                <DeleteIcon />
                            </IconButton>
                        </ListItemSecondaryAction>
                    </ListItem>
                ))}
            </List>
        </Paper>
    );
};
