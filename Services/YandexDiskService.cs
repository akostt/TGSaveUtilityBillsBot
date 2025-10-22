using System.Text;
using Newtonsoft.Json;
using TGSaveUtilityBillsBot.Models;

namespace TGSaveUtilityBillsBot.Services;

public class YandexDiskService
{
    private readonly string _token;
    private readonly string _rootFolder;
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "https://cloud-api.yandex.net/v1/disk/resources";

    public YandexDiskService(string token, string rootFolder)
    {
        _token = token;
        _rootFolder = rootFolder;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {_token}");
    }

    public async Task<bool> UploadFileAsync(BillMetadata metadata, Stream fileStream, string fileName)
    {
        try
        {
            // Создаем путь: Квитанции/2024/Январь/Электричество/
            var folderPath = $"/{_rootFolder}/{metadata.Year}/{metadata.Month}/{metadata.Company}";
            
            // Создаем все необходимые папки
            await CreateFolderAsync(folderPath);

            // Полный путь к файлу
            var filePath = $"{folderPath}/{fileName}";

            // Получаем URL для загрузки
            var uploadUrl = await GetUploadUrlAsync(filePath);
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
            Console.WriteLine($"Ошибка при загрузке файла на Яндекс.Диск: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> GetUploadUrlAsync(string path)
    {
        try
        {
            var url = $"{ApiBaseUrl}/upload?path={Uri.EscapeDataString(path)}&overwrite=true";
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
            Console.WriteLine($"Ошибка при получении URL для загрузки: {ex.Message}");
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
            Console.WriteLine($"Ошибка при создании папки: {ex.Message}");
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

    private class UploadUrlResponse
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }
}

