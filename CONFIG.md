# ⚙️ Конфигурация проекта

Детальное руководство по настройке конфигурации бота.

> 📖 [← Вернуться к README](README.md) | 🚀 [Быстрый старт →](QUICKSTART.md)

---

## Обзор

Проект поддерживает **два способа конфигурации** в зависимости от метода запуска:

- **Docker** → использует переменные окружения (`.env`)
- **Локально** → использует JSON конфигурацию (`appsettings.Development.json`)

---

## 🐳 Конфигурация для Docker

### Создание файла .env

```bash
cp .env.example .env
```

### Формат файла .env

```env
# Токен от @BotFather
TELEGRAM_BOT_TOKEN=123456789:ABCdefGHIjklMNOpqrsTUVwxyz

# OAuth токен Яндекс.Диска
YANDEX_DISK_TOKEN=y0_AgAAAAxxxxxxxxxxxxxxxxxxxxxxx
```

### Как это работает

Docker Compose автоматически преобразует переменные окружения в формат .NET Configuration:

```bash
TELEGRAM_BOT_TOKEN  →  TelegramBot:Token
YANDEX_DISK_TOKEN   →  YandexDisk:Token
```

Это настроено в `docker-compose.yml`:
```yaml
environment:
  - TelegramBot__Token=${TELEGRAM_BOT_TOKEN}
  - YandexDisk__Token=${YANDEX_DISK_TOKEN}
```

---

## 💻 Конфигурация для локального запуска

### Создание файла конфигурации

```bash
cp appsettings.Development.json.example appsettings.Development.json
```

### Формат appsettings.Development.json

```json
{
  "TelegramBot": {
    "Token": "123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
  },
  "YandexDisk": {
    "Token": "y0_AgAAAAxxxxxxxxxxxxxxxxxxxxxxx",
    "RootFolder": "Квитанции"
  }
}
```

### Дополнительные параметры

Вы можете добавить дополнительные настройки:

```json
{
  "TelegramBot": {
    "Token": "ваш_токен"
  },
  "YandexDisk": {
    "Token": "ваш_токен",
    "RootFolder": "Квитанции"  // Можно изменить название корневой папки
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",      // Изменить уровень логирования
      "Microsoft": "Warning"
    }
  }
}
```

---

## 📁 Все конфигурационные файлы

| Файл | Назначение | В Git | Содержит секреты |
|------|------------|-------|------------------|
| `appsettings.json` | Базовая конфигурация | ✅ Да | ❌ Нет |
| `appsettings.Development.json.example` | Пример для локальной разработки | ✅ Да | ❌ Нет |
| `appsettings.Development.json` | **Ваши токены** (локально) | ❌ Нет | ⚠️ **Да** |
| `.env.example` | Пример для Docker | ✅ Да | ❌ Нет |
| `.env` | **Ваши токены** (Docker) | ❌ Нет | ⚠️ **Да** |

---

## 🔒 Безопасность

### ⚠️ Важно!

**Никогда не коммитьте файлы с реальными токенами в Git!**

Файлы `.env` и `appsettings.Development.json` автоматически игнорируются через `.gitignore`:

```gitignore
# Environment variables
.env

# User-specific appsettings (содержат секретные токены)
appsettings.Development.json
```

### Рекомендации по безопасности

1. ✅ Используйте `.example` файлы как шаблоны
2. ✅ Храните токены локально, не делитесь ими
3. ✅ Периодически обновляйте токены
4. ✅ Используйте OAuth токены с минимально необходимыми правами
5. ❌ Не публикуйте токены в публичных репозиториях
6. ❌ Не отправляйте токены в чаты/email

### Что делать если токен утек

1. Немедленно отзовите токен:
   - **Telegram:** создайте новый токен через @BotFather
   - **Яндекс:** отзовите токен в настройках OAuth приложения
2. Создайте новые токены
3. Обновите конфигурацию

---

## 🌍 Переменные окружения в Docker

Docker Compose использует следующую схему преобразования:

```
.env файл                     →    .NET Configuration
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TELEGRAM_BOT_TOKEN            →    TelegramBot__Token
YANDEX_DISK_TOKEN             →    YandexDisk__Token
```

Обратите внимание: двойное подчеркивание `__` используется как разделитель уровней в .NET Configuration.

---

## 🔄 Приоритет конфигурации

.NET загружает конфигурацию в следующем порядке (последующие переопределяют предыдущие):

1. `appsettings.json` (базовые настройки)
2. `appsettings.Development.json` (локальная разработка)
3. Переменные окружения (Docker)
4. Аргументы командной строки

Это означает, что переменные окружения имеют наивысший приоритет.

---

## 📝 Примеры использования

### Изменить название корневой папки на Яндекс.Диске

**В Docker** (`.env`):
```env
TELEGRAM_BOT_TOKEN=...
YANDEX_DISK_TOKEN=...
```

Добавьте в `docker-compose.yml`:
```yaml
environment:
  - YandexDisk__RootFolder=МоиКвитанции
```

**Локально** (`appsettings.Development.json`):
```json
{
  "YandexDisk": {
    "RootFolder": "МоиКвитанции"
  }
}
```

### Включить подробное логирование

**Локально** (`appsettings.Development.json`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "TGSaveUtilityBillsBot": "Trace"
    }
  }
}
```

---

## 🔗 Связанные ресурсы

- 🚀 [QUICKSTART.md](QUICKSTART.md) - пошаговая инструкция по запуску
- 📖 [README.md](README.md) - обзор проекта
- 🐳 [docker-compose.yml](docker-compose.yml) - конфигурация Docker
- 📝 [appsettings.json](appsettings.json) - базовая конфигурация

---

**Вопросы?** Проверьте [QUICKSTART.md](QUICKSTART.md) или раздел "Устранение неполадок"
