import React, { useState } from 'react';
import { TextField, Button, Paper, Box } from '@mui/material';
import { todoApi } from '../api/todoApi';
import { CreateTodoItem } from '../types/todo';

interface TodoFormProps {
    onTodoCreated: () => void;
}

export const TodoForm: React.FC<TodoFormProps> = ({ onTodoCreated }) => {
    const [title, setTitle] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!title.trim()) return;

        try {
            const newTodo: CreateTodoItem = { title: title.trim() };
            await todoApi.create(newTodo);
            setTitle('');
            onTodoCreated();
        } catch (error) {
            console.error('Failed to create todo:', error);
        }
    };

    return (
        <Paper elevation={2} style={{ padding: '16px' }}>
            <form onSubmit={handleSubmit}>
                <Box display="flex" gap={2}>
                    <TextField
                        fullWidth
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="新しいTodoを入力"
                        size="small"
                    />
                    <Button
                        type="submit"
                        variant="contained"
                        color="primary"
                        disabled={!title.trim()}
                    >
                        追加
                    </Button>
                </Box>
            </form>
        </Paper>
    );
};
