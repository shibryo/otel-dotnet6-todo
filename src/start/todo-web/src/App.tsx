import { Container, CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import { TodoList } from './components/TodoList';
import { TodoForm } from './components/TodoForm';

const theme = createTheme({
  palette: {
    mode: 'light',
  },
});

function App() {
  const handleTodoCreated = () => {
    // TodoListコンポーネントのrefreshを自動的に行うため、
    // 特別な処理は不要（useEffectの依存配列なしで自動的にリロード）
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Container maxWidth="md" style={{ marginTop: '2rem' }}>
        <TodoForm onTodoCreated={handleTodoCreated} />
        <TodoList />
      </Container>
    </ThemeProvider>
  );
}

export default App;
