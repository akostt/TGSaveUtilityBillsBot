namespace TGSaveUtilityBillsBot.Constants;

public static class BotMessages
{
    // Приветствия и справка
    public const string Welcome = 
        "👋 Добро пожаловать в бот для сохранения квитанций!\n\n" +
        "Я помогу вам организовать все ваши коммунальные квитанции на Яндекс.Диске.\n\n" +
        "Используйте команду /upload для загрузки квитанции или чека.";

    public const string Help = 
        "📖 Доступные команды:\n\n" +
        "/start - Начать работу с ботом\n" +
        "/upload - Загрузить новую квитанцию\n" +
        "/cancel - Отменить текущую операцию\n" +
        "/help - Показать эту справку";

    // Запросы данных
    public const string AskForYear = "📅 Выберите год документа:";
    public const string AskForManualYear = "✍️ Введите год вручную (например, 2024):";
    public const string AskForMonth = "📆 Выберите месяц:";
    public const string AskForCompany = "🏢 Выберите компанию:";
    public const string AskForDocumentType = "📄 Что вы хотите загрузить?";
    public const string AskForFile = "📎 Теперь отправьте документ в формате PDF.";

    // Ошибки
    public const string InvalidYear = "❌ Некорректный год. Пожалуйста, введите год (например, 2024):";
    public const string InvalidCommand = "❓ Неизвестная команда. Используйте /help для просмотра доступных команд.";
    public const string NotPdfFile = "❌ Пожалуйста, отправьте файл в формате PDF.";
    public const string UseUploadCommand = "Используйте команду /upload для загрузки квитанции.";
    public const string UseUploadCommandFirst = "Сначала используйте команду /upload для начала процесса загрузки.";
    public const string UploadError = "❌ Ошибка при загрузке файла на Яндекс.Диск. Проверьте токен доступа и попробуйте снова.";
    public const string AccessDenied = "🚫 У вас нет доступа к этому боту. Обратитесь к администратору.";

    // Успех и отмена
    public const string OperationCancelled = "❌ Операция отменена.";
    public const string Uploading = "⏳ Загружаю файл на Яндекс.Диск...";
    public const string Overwriting = "🔄 Перезаписываю файл на Яндекс.Диске...";
    
    public static string UploadSuccess(int year, string month, string company, string fileName) =>
        $"✅ Квитанция успешно сохранена!\n\n" +
        $"📁 Путь: Квитанции/{year}/{month}/{company}/{fileName}\n\n" +
        $"Используйте /upload для загрузки следующей квитанции.";
    
    public static string MonthSelected(string month) =>
        $"✅ Выбран месяц: {month}\n\n🏢 Выберите компанию:";
    
    public static string CompanySelected(string company) =>
        $"✅ Выбрана компания: {company}\n\n📄 Что вы хотите загрузить?";
    
    public static string DocumentTypeSelected(int year, string month, string company, string documentType) =>
        $"✅ Год: {year}\n" +
        $"✅ Месяц: {month}\n" +
        $"✅ Компания: {company}\n" +
        $"✅ Тип документа: {documentType}\n\n" +
        $"📎 Теперь отправьте документ в формате PDF.";
    
    public static string Error(string message) => $"❌ Произошла ошибка: {message}";
    
    public static string FileExists(string fileName) =>
        $"⚠️ Файл '{fileName}' уже существует.\n\n" +
        $"Что вы хотите сделать?";
}

