using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Bot.Sharding;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using JazzMock.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Qmmands;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace JazzMock
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(x =>
                {
                    x.AddCommandLine(args);
                })
                .ConfigureAppConfiguration(x =>
                {
                    x.AddCommandLine(args);
                    x.AddEnvironmentVariables("DISQORD_");
                })
                .ConfigureLogging(x =>
                {
                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code)
                        .WriteTo.File($"logs/log-{DateTime.Now:HH_mm_ss}.txt", restrictedToMinimumLevel: LogEventLevel.Verbose, fileSizeLimitBytes: null, buffered: true)
                        .CreateLogger();
                    x.AddSerilog(logger, true);

                    x.Services.Remove(x.Services.First(x => x.ServiceType == typeof(ILogger<>)));
                    x.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddInteractivity();
                })
                .ConfigureDiscordBotSharder((context, bot) =>
                {
                    bot.Token = context.Configuration["TOKEN"];
                    bot.UseMentionPrefix = true;
                    bot.Intents += GatewayIntent.DirectMessages;
                    bot.Prefixes = new[] { "jazzis" };
                    bot.ReadyEventDelayMode = ReadyEventDelayMode.Guilds;
                    bot.ShardCount = 1;
                })
                .Build();
            
            try
            {
                using (host)
                {
                    host.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }
    }
}