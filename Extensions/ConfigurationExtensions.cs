using Microsoft.Extensions.Configuration;

namespace TGSaveUtilityBillsBot.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddTelegramBotConfiguration(this IConfigurationBuilder builder)
    {
        return builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    }
}



