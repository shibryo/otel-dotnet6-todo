docker_compose('tilt/docker-compose.yml')

# Live update configuration for the app
docker_build(
    'otel-dotnet6-todo-app',
    context='.',
    dockerfile='tilt/Dockerfile',
    live_update=[
        sync('.', '/workspace'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj'])
    ]
)

# Port forwards
k8s_yaml('tilt/docker-compose.yml')  # Tilt will extract port information from compose file
