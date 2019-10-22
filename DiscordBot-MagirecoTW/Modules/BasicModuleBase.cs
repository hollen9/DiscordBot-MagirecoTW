using Discord;
using Discord.Commands;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MitamaBot.Modules
{
    public class BasicModuleBase : ModuleBase<SocketCommandContext>
    {
        public Task<IUserMessage> ReplyEmbedAsync(Embed embed)
            => ReplyAsync(string.Empty, false, embed, null);
        public Task<IUserMessage> ReplyEmbedAsync(EmbedBuilder builder)
            => ReplyAsync(string.Empty, false, builder.Build(), null);

        public Task ReplyReactionAsync(IEmote emote)
            => Context.Message.AddReactionAsync(emote);

        public Task<RestUserMessage> ReplyFileAsync(Stream stream, string fileName, string message = null)
            => Context.Channel.SendFileAsync(stream, fileName, message, false, null);

        public Task<IUserMessage> ReplyMentionAsync(string content)
            => ReplyAsync($"{Context.User.Mention} {content}", false, null, null);

        //public Task ModifyIfPossible(this IUserMessage msg, Action<MessageProperties> msgProperties)
        //{
        //    var msgP = new MessageProperties();
        //    msgProperties(msgP);

        //    //Context.Guild.CurrentUser.GuildPermissions.

        //    msg.ModifyAsync(x => x = msgP);
        //}
    }
}
