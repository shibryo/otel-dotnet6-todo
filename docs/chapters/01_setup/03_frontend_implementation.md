# フロントエンドの実装

このセクションでは、TodoアプリケーションのフロントエンドをReactを使用して実装します。

## プロジェクトの作成

### Reactプロジェクトの作成

1. Viteを使用してプロジェクトを作成
```bash
npm create vite@latest todo-web -- --template react-ts
cd todo-web
```

2. 必要なパッケージのインストール
```bash
npm install
npm install @mui/material @emotion/react @emotion/styled axios
```

## プロジェクト構造の整備

以下のような構造でファイルを作成します：

```
src/
├── api/
│   └── todoApi.ts
├── components/
│   ├── TodoForm.tsx
│   └── TodoList.tsx
├── types/
│   └── todo.ts
└── App.tsx
```

## 型定義の実装

### Todo型の定義

`src/types/todo.ts`を作成し、以下の内容を実装します：

```typescript
export interface Todo {
  id: number;
  title: string;
  isComplete: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface TodoCreate {
  title: string;
  isComplete: boolean;
}
```

## APIクライアントの実装

### TodoAPIクライアントの作成

`src/api/todoApi.ts`を作成し、以下の内容を実装します：

```typescript
import axios from 'axios';
import { Todo, TodoCreate } from '../types/todo';

const API_BASE_URL = 'http://localhost:5000/api';

export const todoApi = {
  // Todo一覧の取得
  async getTodos(): Promise<Todo[]> {
    const response = await axios.get<Todo[]>(`${API_BASE_URL}/TodoItems`);
    return response.data;
  },

  // Todo項目の作成
  async createTodo(todo: TodoCreate): Promise<Todo> {
    const response = await axios.post<Todo>(`${API_BASE_URL}/TodoItems`, todo);
    return response.data;
  },

  // Todo項目の更新
  async updateTodo(id: number, todo: Todo): Promise<void> {
    await axios.put(`${API_BASE_URL}/TodoItems/${id}`, todo);
  },

  // Todo項目の削除
  async deleteTodo(id: number): Promise<void> {
    await axios.delete(`${API_BASE_URL}/TodoItems/${id}`);
  }
};
```

## コンポーネントの実装

### TodoFormコンポーネント

`src/components/TodoForm.tsx`を作成し、以下の内容を実装します：

```typescript
import React, { useState } from 'react';
import { Button, TextField, Box } from '@mui/material';
import { todoApi } from '../api/todoApi';
import { TodoCreate } from '../types/todo';

interface Props {
  onTodoCreated: () => void;
}

export const TodoForm: React.FC<Props> = ({ onTodoCreated }) => {
  const [title, setTitle] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    const newTodo: TodoCreate = {
      title: title.trim(),
      isComplete: false
    };

    try {
      await todoApi.createTodo(newTodo);
      setTitle('');
      onTodoCreated();
    } catch (error) {
      console.error('Failed to create todo:', error);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ mb: 3 }}>
      <TextField
        fullWidth
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder="新しいタスクを入力"
        sx={{ mr: 1 }}
      />
      <Button
        type="submit"
        variant="contained"
        sx={{ mt: 1 }}
        disabled={!title.trim()}
      >
        追加
      </Button>
    </Box>
  );
};
```

### TodoListコンポーネント

`src/components/TodoList.tsx`を作成し、以下の内容を実装します：

```typescript
import React from 'react';
import {
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Checkbox,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { Todo } from '../types/todo';
import { todoApi } from '../api/todoApi';

interface Props {
  todos: Todo[];
  onTodoUpdated: () => void;
}

export const TodoList: React.FC<Props> = ({ todos, onTodoUpdated }) => {
  const handleToggle = async (todo: Todo) => {
    try {
      await todoApi.updateTodo(todo.id, {
        ...todo,
        isComplete: !todo.isComplete
      });
      onTodoUpdated();
    } catch (error) {
      console.error('Failed to update todo:', error);
    }
  };

  const handleDelete = async (id: number) => {
    try {
      await todoApi.deleteTodo(id);
      onTodoUpdated();
    } catch (error) {
      console.error('Failed to delete todo:', error);
    }
  };

  return (
    <List>
      {todos.map((todo) => (
        <ListItem key={todo.id} dense>
          <Checkbox
            edge="start"
            checked={todo.isComplete}
            onChange={() => handleToggle(todo)}
          />
          <ListItemText
            primary={todo.title}
            sx={{
              textDecoration: todo.isComplete ? 'line-through' : 'none',
              color: todo.isComplete ? 'text.secondary' : 'text.primary'
            }}
          />
          <ListItemSecondaryAction>
            <IconButton
              edge="end"
              onClick={() => handleDelete(todo.id)}
            >
              <DeleteIcon />
            </IconButton>
          </ListItemSecondaryAction>
        </ListItem>
      ))}
    </List>
  );
};
```

### Appコンポーネント

`src/App.tsx`を以下のように更新します：

```typescript
import { useEffect, useState } from 'react';
import { Container, Typography, Paper, Box } from '@mui/material';
import { TodoForm } from './components/TodoForm';
import { TodoList } from './components/TodoList';
import { Todo } from './types/todo';
import { todoApi } from './api/todoApi';
import './App.css';

function App() {
  const [todos, setTodos] = useState<Todo[]>([]);

  const fetchTodos = async () => {
    try {
      const data = await todoApi.getTodos();
      setTodos(data);
    } catch (error) {
      console.error('Failed to fetch todos:', error);
    }
  };

  useEffect(() => {
    fetchTodos();
  }, []);

  return (
    <Container maxWidth="sm">
      <Box sx={{ my: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Todoリスト
        </Typography>
        <Paper sx={{ p: 2 }}>
          <TodoForm onTodoCreated={fetchTodos} />
          <TodoList todos={todos} onTodoUpdated={fetchTodos} />
        </Paper>
      </Box>
    </Container>
  );
}

export default App;
```

## スタイリングの追加

### CSSの修正

`src/App.css`を以下のように更新します：

```css
#root {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
}

body {
  background-color: #f5f5f5;
}
```

## 開発環境の設定

### Vite設定の更新

`vite.config.ts`を以下のように更新します：

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
  },
});
```

## アプリケーションの起動

1. 開発サーバーの起動
```bash
npm run dev
```

2. ブラウザでアプリケーションにアクセス
- http://localhost:3000 を開く

## トラブルシューティング

### よくある問題と解決方法

1. CORS エラー
- バックエンドのCORS設定の確認
- ブラウザのコンソールでエラーメッセージの確認

2. APIエンドポイントの接続エラー
- バックエンドサーバーの起動確認
- API_BASE_URLの設定確認
- ネットワーク接続の確認

3. コンポーネントの表示問題
- Reactコンポーネントのレンダリングエラーの確認
- 必要なパッケージのインストール確認
- TypeScriptの型エラーの解決

## 次のステップ

フロントエンドの実装が完了したら、[動作確認とトラブルシューティング](./04_testing_and_troubleshooting.md)に進みます。
