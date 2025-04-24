-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create schema
CREATE SCHEMA IF NOT EXISTS todo;

-- Create todos table
CREATE TABLE IF NOT EXISTS todo.todos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    due_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create function for updating updated_at timestamp
CREATE OR REPLACE FUNCTION todo.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for automatically updating updated_at
CREATE TRIGGER update_todo_updated_at
    BEFORE UPDATE ON todo.todos
    FOR EACH ROW
    EXECUTE FUNCTION todo.update_updated_at_column();

-- Add indexes
CREATE INDEX IF NOT EXISTS idx_todos_is_completed ON todo.todos(is_completed);
CREATE INDEX IF NOT EXISTS idx_todos_due_date ON todo.todos(due_date);

-- Add sample data
INSERT INTO todo.todos (title, description, due_date)
VALUES 
    ('Implement OpenTelemetry', 'Setup and configure OpenTelemetry for the Todo application', CURRENT_TIMESTAMP + INTERVAL '7 days'),
    ('Write integration tests', 'Create comprehensive integration tests for the API endpoints', CURRENT_TIMESTAMP + INTERVAL '3 days'),
    ('Setup CI/CD pipeline', 'Configure GitHub Actions for continuous integration and deployment', CURRENT_TIMESTAMP + INTERVAL '5 days')
ON CONFLICT DO NOTHING;
