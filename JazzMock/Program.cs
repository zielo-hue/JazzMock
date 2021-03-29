using System;
using System.IO;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Prefixes;
using Disqord.Bot.Sharding;
using JazzMock.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Qmmands;

namespace JazzMock
{
    internal class Program : DiscordBot
    {
        private static readonly JObject Conf = JObject.Parse(File.ReadAllText(@"config.json"));
        private static readonly string BotToken = (string) Conf["DISCORD"]?["TOKEN"];

        private static void Main()
            => new Program().Run();

        private Program() : base(TokenType.Bot, BotToken,
            new DefaultPrefixProvider()
                .AddPrefix("jazzis"),
            new DiscordBotConfiguration()
            {
                Status = UserStatus.Online,
                ProviderFactory = bot =>
                    new ServiceCollection()
                        .AddSingleton((DiscordBot) bot)
                        .AddSingleton(new GptGeneratorService())
                        .BuildServiceProvider(),
            })
        {
            CommandExecutionFailed += Handler;

            AddModules(typeof(Program).Assembly);
        }
        
        // In case of practical problem:
        private Task Handler(CommandExecutionFailedEventArgs args)
        {
            Console.WriteLine(args.Result.Exception.ToString());
            throw args.Result.Exception;
        }
    }
}