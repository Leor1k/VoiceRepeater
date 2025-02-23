FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем .csproj и восстанавливаем зависимости
COPY ./VoiceModul.csproj ./
RUN dotnet restore

# Копируем остальные файлы и собираем проект
COPY . ./
RUN dotnet build -c Debug -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build ./
ENTRYPOINT ["dotnet", "VoiceModul.dll"]
