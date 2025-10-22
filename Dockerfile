# Используем официальный образ .NET 9.0 SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY TGSaveUtilityBillsBot.csproj .
RUN dotnet restore

# Копируем все файлы и собираем проект
COPY . .
RUN dotnet build -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Используем runtime образ для финального контейнера
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Создаем непривилегированного пользователя
RUN useradd -m -u 1000 botuser && chown -R botuser:botuser /app
USER botuser

# Запускаем приложение
ENTRYPOINT ["dotnet", "TGSaveUtilityBillsBot.dll"]

