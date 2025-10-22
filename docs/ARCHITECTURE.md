# 🏗️ Архитектура проекта

Этот документ описывает архитектурные решения и паттерны, использованные в проекте.

> 📖 [← Вернуться к README](../README.md) | 🚀 [Быстрый старт →](QUICKSTART.md) | ⚙️ [Конфигурация →](CONFIG.md)

---

## Принципы проектирования

### SOLID Принципы

1. **Single Responsibility (SRP)** ✅
   - Каждый класс отвечает за одну задачу
   - `YandexDiskService` - работа с API Яндекс.Диска
   - `UserStateManager` - управление состояниями пользователей
   - `KeyboardFactory` - создание клавиатур
   - `BotHandlers` - обработка событий бота

2. **Open/Closed (OCP)** ✅
   - Классы открыты для расширения через интерфейсы
   - Закрыты для модификации

3. **Liskov Substitution (LSP)** ✅
   - Все реализации соответствуют контрактам интерфейсов

4. **Interface Segregation (ISP)** ✅
   - Интерфейсы узкоспециализированные
   - `IYandexDiskService`, `IUserStateManager`, `IKeyboardFactory`

5. **Dependency Inversion (DIP)** ✅
   - Зависимости через интерфейсы
   - Использование DI контейнера

## Структура проекта

```
TGSaveUtilityBillsBot/
├── Configuration/          # Строго типизированные настройки
│   ├── TelegramBotOptions.cs
│   └── YandexDiskOptions.cs
│
├── Constants/              # Константы приложения
│   ├── BotMessages.cs
│   ├── BotCommands.cs
│   ├── CallbackDataPrefixes.cs
│   └── ValidationRules.cs
│
├── Extensions/             # Extension методы
│   ├── ConfigurationExtensions.cs
│   └── ServiceCollectionExtensions.cs
│
├── Handlers/               # Обработчики событий
│   └── BotHandlers.cs
│
├── Interfaces/             # Контракты
│   ├── IYandexDiskService.cs
│   ├── IUserStateManager.cs
│   └── IKeyboardFactory.cs
│
├── Models/                 # Модели данных
│   ├── BillMetadata.cs
│   ├── Company.cs
│   ├── DocumentType.cs
│   ├── Month.cs
│   └── UserState.cs
│
├── Services/               # Реализации сервисов
│   ├── KeyboardFactory.cs
│   ├── TelegramBotHostedService.cs
│   ├── UserStateManager.cs
│   └── YandexDiskService.cs
│
├── Program.cs              # Точка входа
└── TelegramBot.cs          # Главный класс бота
```

## Архитектурные паттерны

### 1. Dependency Injection (DI)

Все зависимости внедряются через конструктор:

```csharp
public class YandexDiskService : IYandexDiskService
{
    public YandexDiskService(
        IOptions<YandexDiskOptions> options,
        ILogger<YandexDiskService> logger)
    {
        // ...
    }
}
```

**Преимущества:**
- Легкое тестирование
- Слабая связанность
- Инверсия контроля

### 2. Options Pattern

Строго типизированная конфигурация:

```csharp
public class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";
    
    [Required]
    public string Token { get; set; }
    public string AllowedUserIds { get; set; }
}
```

**Преимущества:**
- Валидация на старте приложения
- IntelliSense в IDE
- Type-safe доступ к настройкам

### 3. Hosted Service Pattern

Фоновый сервис с graceful shutdown:

```csharp
public class TelegramBotHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) { }
    public Task StopAsync(CancellationToken cancellationToken) { }
}
```

**Преимущества:**
- Автоматическое управление жизненным циклом
- Graceful shutdown при Ctrl+C
- Интеграция с .NET Generic Host

### 4. Factory Pattern

Создание клавиатур через фабрику:

```csharp
public interface IKeyboardFactory
{
    InlineKeyboardMarkup CreateYearKeyboard();
    InlineKeyboardMarkup CreateMonthKeyboard();
    InlineKeyboardMarkup CreateCompanyKeyboard();
    InlineKeyboardMarkup CreateDocumentTypeKeyboard();
    InlineKeyboardMarkup CreateOverwriteConfirmationKeyboard();
}
```

**Преимущества:**
- Единое место создания клавиатур
- Легко модифицировать
- Переиспользование логики

### 5. State Pattern

Управление состоянием диалога:

```csharp
public enum UserStateEnum
{
    None,
    WaitingForYear,           // Показана клавиатура выбора года
    WaitingForManualYear,     // Ожидание ручного ввода года
    WaitingForMonth,
    WaitingForCompany,
    WaitingForDocumentType,
    WaitingForFile,
    WaitingForOverwriteConfirmation
}
```

**Преимущества:**
- Четкий flow взаимодействия
- Валидация на каждом шаге
- Изоляция состояний пользователей
- Гибкость: можно выбрать через UI или ввести вручную

## Потокобезопасность

### ConcurrentDictionary

```csharp
public class UserStateManager : IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserState> _userStates = new();
}
```

**Зачем:**
- Бот обрабатывает множество пользователей одновременно
- ConcurrentDictionary обеспечивает потокобезопасность
- Не нужны блокировки (lock)

## Логирование

### Structured Logging

Проект использует минималистичный подход к логированию - логируются только критические события и ошибки:

```csharp
// Логируем только ошибки с контекстом
_logger.LogError(ex, "Ошибка при загрузке файла {FileName} на Яндекс.Диск", fileName);
```

**Уровни логирования:**
- `Information` - запуск/остановка бота, важные события
- `Error` - ошибки с полным контекстом
- `Critical` - критические сбои приложения

**Философия:**
- Избыточное логирование замедляет работу и засоряет логи
- Логируем только то, что действительно важно для диагностики
- Используем структурированное логирование с параметрами

## Обработка ошибок

### Стратегия обработки

1. **Валидация на входе**
   ```csharp
   ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
   ```

2. **Try-Catch с логированием**
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(ex, "Контекст ошибки");
       return false;
   }
   ```

3. **Пользовательские сообщения**
   ```csharp
   await botClient.SendTextMessageAsync(
       chatId,
       BotMessages.Error(ex.Message)
   );
   ```

## Конфигурация

### Два способа настройки

**Docker (переменные окружения):**
```env
TELEGRAM_BOT_TOKEN=...
ALLOWED_USER_IDS=123,456
```

**Локально (JSON):**
```json
{
  "TelegramBot": {
    "Token": "...",
    "AllowedUserIds": [123, 456]
  }
}
```

### Приоритет загрузки

1. `appsettings.json` (базовая)
2. `appsettings.Development.json` (override)
3. Environment Variables (highest priority)

## Безопасность

### 1. Белый список пользователей

```csharp
if (_allowedUserIds.Count > 0 && !_allowedUserIds.Contains(userId))
{
    // Отказ в доступе
}
```

### 2. Валидация входных данных

```csharp
if (!int.TryParse(message.Text, out var year) || 
    year < ValidationRules.MinYear || 
    year > ValidationRules.MaxYear)
{
    // Ошибка валидации
}
```

### 3. Проверка типов файлов

```csharp
if (!document.FileName!.EndsWith(
    ValidationRules.PdfExtension, 
    StringComparison.OrdinalIgnoreCase))
{
    // Отклонить
}
```

## Масштабируемость

### Возможности расширения

1. **Добавление новых команд**
   - Добавить в `BotCommands`
   - Добавить case в `HandleCommandAsync`

2. **Новые типы документов**
   - Добавить в enum `DocumentType`
   - Автоматически появится в клавиатуре

3. **Новые компании**
   - Добавить в enum `Company`
   - Автоматически появится в клавиатуре

4. **Настройка диапазона года**
   - Изменить логику в `CreateYearKeyboard()`
   - Например: показывать последние 5 лет

5. **Интеграция других хранилищ**
   - Создать новый класс, реализующий `IYandexDiskService`
   - Заменить в DI контейнере

## Производительность

### Оптимизации

1. **Переиспользование HttpClient**
   ```csharp
   private readonly HttpClient _httpClient = new();
   ```

2. **Потоковая загрузка файлов**
   ```csharp
   using var memoryStream = new MemoryStream();
   await botClient.DownloadFileAsync(..., memoryStream);
   ```

3. **Асинхронность везде**
   - Все IO операции асинхронные
   - Использование `async/await`

## Тестируемость

### Что можно тестировать

1. **Unit тесты**
   - `UserStateManager`
   - `KeyboardFactory`
   - `TelegramBotOptions.GetAllowedUserIds()`

2. **Integration тесты**
   - `YandexDiskService` (с mock HTTP)
   - `BotHandlers` (с mock bot client)

3. **End-to-End тесты**
   - Полный flow загрузки квитанции

## Best Practices

✅ **Используется:**
- Async/await для IO операций
- Using для IDisposable
- Nullable reference types
- Pattern matching
- Expression-bodied members
- Record types для константы

❌ **Избегаем:**
- Blocking calls (`.Result`, `.Wait()`)
- Глобальное состояние
- Магические строки/числа
- Catch без логирования
- Игнорирование исключений

## Дальнейшее развитие

### Возможные улучшения

1. **Персистентное хранилище состояний**
   - Redis для состояний пользователей
   - Выживание при перезапуске

2. **Метрики и мониторинг**
   - Prometheus metrics
   - Health checks
   - Application Insights

3. **Rate Limiting**
   - Ограничение запросов от пользователя
   - Защита от спама

4. **Многоязычность**
   - Локализация сообщений
   - Выбор языка пользователем

5. **Кэширование**
   - Кэш результатов API
   - Уменьшение нагрузки

---

## 📚 Связанные документы

- 📖 [README.md](../README.md) - обзор проекта и возможностей
- 🚀 [QUICKSTART.md](QUICKSTART.md) - быстрый старт
- ⚙️ [CONFIG.md](CONFIG.md) - настройка конфигурации

---

**Версия:** 1.2  
**Последнее обновление:** 2025-10-22  
**Изменения:** Реорганизация структуры проекта, оптимизация документации, улучшение UX

