using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TGSaveUtilityBillsBot.Configuration;
using TGSaveUtilityBillsBot.Handlers;
using TGSaveUtilityBillsBot.Interfaces;
using TGSaveUtilityBillsBot.Services;

namespace TGSaveUtilityBillsBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация конфигурации
        services.AddOptions<TelegramBotOptions>()
            .Bind(configuration.GetSection(TelegramBotOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<YandexDiskOptions>()
            .Bind(configuration.GetSection(YandexDiskOptions.SectionName))
            .ValidateOnStart();

        // Регистрация сервисов
        services.AddSingleton<IYandexDiskService, YandexDiskService>();
        services.AddSingleton<IUserStateManager, UserStateManager>();
        services.AddSingleton<IKeyboardFactory, KeyboardFactory>();
        services.AddSingleton<BotHandlers>();
        services.AddSingleton<TelegramBot>();

        // Регистрация Hosted Service
        services.AddHostedService<TelegramBotHostedService>();

        return services;
    }
}

