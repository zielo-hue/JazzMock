using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using JazzMock.Services;
using Qmmands;

namespace JazzMock.Modules
{
    public class MainModule : DiscordModuleBase
    {
        public DiscordBot Bot { get; set; }
        public GptGeneratorService GptGeneratorService { get; set; }

        [Command("help", "info")]
        [Description("Displays information about the bot.")]
        public async Task HelpAsync()
        {
            var embed = new LocalEmbedBuilder()
                .WithTitle(Bot.CurrentUser.Name)
                .WithDescription("powere b gpt-2 n shiny new disqord...");
            await Response(embed);
        }
    }
}