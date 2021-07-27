using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using JazzMock.Services;
using Qmmands;

namespace JazzMock.Modules
{
    public class MainModule : DiscordModuleBase
    {
        public GptGeneratorService GptGeneratorService { get; set; }

        [Command("help", "info")]
        [Description("Displays information about the bot.")]
        public async Task HelpAsync()
        {
            var embed = new LocalEmbed()
                .WithTitle(GptGeneratorService.Client.CurrentUser.Name)
                .WithDescription("powere b gpt-2 n shiny new disqord...");
            await Response(embed);
        }

        [Command("genconvo", "generate", "gen")]
        [Description("Generate a conversation.")]
        [Cooldown(1, 15, CooldownMeasure.Seconds, CooldownBucketType.Guild)]
        public async Task GenConvoAsync(string prefix = "")
        {
            await Reaction(new LocalEmoji("🚮"));
            var embed = new LocalEmbed()
                .WithTitle("gen ben len");
            var genText = await GptGeneratorService.GenerateMessage(prefix, 0b01);
            embed.WithDescription(genText).WithFooter($"requested by {Context.Author.Name}", Context.Author.GetAvatarUrl());
            await Reply(embed);
        }
    }
}