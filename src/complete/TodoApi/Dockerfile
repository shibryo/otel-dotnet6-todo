FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# ソースコードをコピー
COPY ["TodoApi.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet build "TodoApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TodoApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]
