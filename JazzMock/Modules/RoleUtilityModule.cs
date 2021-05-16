using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Disqord.Rest.Default;
using Qmmands;
using Color = Disqord.Color;

// ReSharper disable PossibleInvalidOperationException

namespace JazzMock.Modules
{
    public class RoleUtilityModule : DiscordGuildModuleBase
    {
        // thanks to https://github.com/AnotherZane for making my code 3x slimmer and functional
        [Command("rolecolor", "rclr", "rc", "color", "clr")]
        [Description("Changes or adds a color for your role.")]
        [Cooldown(1, 30, CooldownMeasure.Seconds, CooldownBucketType.User)]
        [RequireBotGuildPermissions(Permission.ManageRoles)]
        public async Task RoleColorAsync(Color color)
        {
            var role = Context.Guild.Roles.Values.FirstOrDefault(x => x.Name == color.ToString());
            if (role is null)
            {
                role = await Context.Guild.CreateRoleAsync(x =>
                {
                    x.Color = color;
                    x.Name = color.ToString();
                });
            }
            if (Context.Author.RoleIds.Contains(role.Id)) // TODO: add a condition that checks if unconventional role has the color
            {
                await Reply(new LocalEmbedBuilder()
                    .WithTitle("no need")
                    .WithDescription("you already have color"));
            }
            else
            {
                await Context.Guild.GrantRoleAsync(Context.Author.Id, role.Id, new DefaultRestRequestOptions());
                await Reply(new LocalEmbedBuilder()
                    .WithTitle("role added"));
            }
        }

        [Command("deleterole")]
        [RequireBotGuildPermissions(Permission.ManageRoles)]
        [RequireAuthorGuildPermissions(Permission.ManageRoles)]
        public async Task DeleteRoleAsync(Snowflake roleid)
        {
            var embed = new LocalEmbedBuilder();
            try
            {
                await Context.Guild.DeleteRoleAsync(roleid, new DefaultRestRequestOptions());
                embed.WithTitle("ok");
            }
            catch
            {
                embed.WithTitle("no");
            }

            await Reply(embed);
        }

        [Command("removerole", "rmrole")]
        [RequireBotGuildPermissions(Permission.ManageRoles)]
        public async Task RemoveRoleAsync(Color color)
        {
            var embed = new LocalEmbedBuilder();
            var roleMatch = Context.Guild.Roles.Values.FirstOrDefault(x => x.Name == color.ToString());
            if (roleMatch is null)
                embed.WithTitle("you dont have that color");
            else
            {
                await Context.Guild.RevokeRoleAsync(Context.Author.Id, roleMatch.Id, new DefaultRestRequestOptions());
                embed.WithTitle($"ok, removed {roleMatch.Name} of id {roleMatch.Id}");
                embed.WithColor(roleMatch.Color);
            }

            await Reply(embed);
        }
    }
}