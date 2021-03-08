using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Qmmands;

namespace JazzMock.Modules
{
    public class MainModule : DiscordModuleBase
    {
        public DiscordBot Bot { get; set; }

        [Command("help", "info")]
        [Description("Displays information about the bot.")]
        public Task HelpAsync()
            => ReplyAsync(embed: new LocalEmbedBuilder()
                .WithTitle("jazzmom")
                .WithDescription("powere b gpt-2 n tensorflow.NET....")
                .WithColor(Color.Purple)
                .Build());
        
        
    }
}