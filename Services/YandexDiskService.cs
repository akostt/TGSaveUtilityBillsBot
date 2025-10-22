using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TGSaveUtilityBillsBot.Configuration;
using TGSaveUtilityBillsBot.Interfaces;
using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Services;

public class YandexDiskService : IYandexDiskService
{
    private readonly string _token;
    private readonly string _rootFolder;
    private readonly HttpClient _httpClient;
    private readonly ILogger<YandexDiskService> _logger;
    private const string ApiBaseUrl = "https://cloud-api.yandex.net/v1/disk/resources";

    public YandexDiskService(IOptions<YandexDiskOptions> options, ILogger<YandexDiskService> logger)
    {
        var config = options.Value;
        _token = config.Token;
        _rootFolder = config.RootFolder;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {_token}");
    }

    public async Task<bool> UploadFileAsync(BillMetadata metadata, Stream fileStream, string fileName, bool overwrite = false)
    {
        try
        {
            // Создаем путь: Квитанции/2024/Январь/Электричество/
            var folderPath = $"/{_rootFolder}/{metadata.Year}/{metadata.Month}/{metadata.Company}";
            
            // Создаем все необходимые папки
            await CreateFolderAsync(folderPath);

            // Полный путь к файлу
            var filePath = $"{folderPath}/{fileName}";

            // Если нужна перезапись - сначала удаляем существующий файл
            if (overwrite)
            {
                await DeleteFileAsync(filePath);
            }

            // Получаем URL для загрузки
            var uploadUrl = await GetUploadUrlAsync(filePath, false);
            if (string.IsNullOrEmpty(uploadUrl))
            {
                return false;
            }

            // Загружаем файл
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            
            var response = await _httpClient.PutAsync(uploadUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке файла {FileName} на Яндекс.Диск", fileName);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(BillMetadata metadata, string fileName)
    {
        try
        {
            var folderPath = $"/{_rootFolder}/{metadata.Year}/{metadata.Month}/{metadata.Company}";
            var filePath = $"{folderPath}/{fileName}";
            
            var url = $"{ApiBaseUrl}?path={Uri.EscapeDataString(filePath)}";
            var response = await _httpClient.GetAsync(url);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetUploadUrlAsync(string path, bool overwrite = false)
    {
        try
        {
            var url = $"{ApiBaseUrl}/upload?path={Uri.EscapeDataString(path)}&overwrite={overwrite.ToString().ToLower()}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<UploadUrlResponse>(json);
            return uploadInfo?.Href;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении URL для загрузки файла {Path}", path);
            return null;
        }
    }

    private async Task<bool> CreateFolderAsync(string path)
    {
        try
        {
            // Создаем все папки в иерархии
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            foreach (var part in parts)
            {
                currentPath += "/" + part;
                await CreateSingleFolderAsync(currentPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании папки {Path}", path);
            return false;
        }
    }

    private async Task<bool> CreateSingleFolderAsync(string path)
    {
        try
        {
            var url = $"{ApiBaseUrl}?path={Uri.EscapeDataString(path)}";
            var response = await _httpClient.PutAsync(url, null);
            
            // 201 - папка создана, 409 - папка уже существует
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> DeleteFileAsync(string path)
    {
        try
        {
            var url = $"{ApiBaseUrl}?path={Uri.EscapeDataString(path)}&permanently=true";
            var response = await _httpClient.DeleteAsync(url);
            
            // 204 - файл удалён, 404 - файл не существует (это ок для нас)
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении файла {Path}", path);
            return false;
        }
    }

    private class UploadUrlResponse
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }
}

