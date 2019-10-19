using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MitamaBot.Services
{
    public class ResponsiveService
    {
        private readonly ILogger<ResponsiveService> _logger;
        private readonly ResponsiveOptions _options;
        private readonly DiscordSocketClient _discord;

        public ResponsiveService(/*ILogger<ResponsiveService> logger, */IConfiguration config, DiscordSocketClient discord)
        {
            //_logger = logger;
            _discord = discord;
            
            var options = new ResponsiveOptions();
            config.Bind("responsive", options);
            _options = options;
        }

        private async Task<T> WaitAsync<T>(TaskCompletionSource<T> tcs, TimeSpan? expireAfter = null)
        {
            new Timer((s) => tcs.TrySetCanceled(), null, expireAfter == null ? TimeSpan.FromSeconds(_options.DefaultExpireSeconds) : (TimeSpan)expireAfter, TimeSpan.Zero);
            try
            {
                return await tcs.Task;
            }
            catch (Exception)
            {
                //_logger.LogInformation("Cancelled task after {0} seconds with no reply", _options.DefaultExpireSeconds);
            }
            return default;
        }

        public async Task<SocketMessage> WaitForMessageAsync(Func<SocketMessage, bool> condition, TimeSpan? expireAfter = null, TaskCompletionSource<SocketMessage> tcs = null)
        {
            if (tcs == null) tcs = new TaskCompletionSource<SocketMessage>();

            _discord.MessageReceived += (msg) =>
            {
                if (condition(msg))
                    tcs.TrySetResult(msg);
                return Task.CompletedTask;
            };

            return await WaitAsync(tcs, expireAfter);
        }

        public async Task<SocketReaction> WaitForReactionAsync(Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, bool> condition, TimeSpan? expireAfter = null, TaskCompletionSource<SocketReaction> tcs = null)
        {
            if (tcs == null) tcs = new TaskCompletionSource<SocketReaction>();

            _discord.ReactionAdded += (cache, ch, r) =>
            {
                if (condition(cache, ch, r))
                    tcs.TrySetResult(r);
                return Task.CompletedTask;
            };

            return await WaitAsync(tcs, expireAfter);
        }

        /// <summary>
        /// <para>若逾時未作答回傳 null。</para>
        /// <para>取消回傳 int.MaxValue。</para>
        /// <para>不明原因回傳 int.MinValue。</para>
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="optionEmojis">這可以呼叫 GetNumberOptionsEmojis() </param>
        /// <param name="startNumber"></param>
        /// <param name="isCancellable"></param>
        /// <param name="msgCancelKeywords"></param>
        /// <param name="expireAfter"></param>
        /// <param name="tcs"></param>
        /// <returns></returns>
        public async Task<int?> WaitForNumberAnswerAsync(
            ulong channelId,
            List<Emoji> optionEmojis,
            byte startNumber = 1,
            bool isCancellable = false,
            string[] msgCancelKeywords = null,
            TimeSpan? expireAfter = null,
            TaskCompletionSource<SocketMessageOrReaction> tcs = null)
        {
            if (tcs == null)
            {
                tcs = new TaskCompletionSource<SocketMessageOrReaction>();
            }
            
            int? userChoose = null;

            _discord.MessageReceived += (x) =>
            {
                if (x.Channel.Id != channelId || x.Author.IsBot || x.Author.IsWebhook)
                {
                    return Task.CompletedTask;
                }
                string content = x.Content.Trim();

                if (isCancellable && msgCancelKeywords != null && msgCancelKeywords.Contains(content))
                {
                    userChoose = int.MaxValue;
                    tcs.TrySetResult(new SocketMessageOrReaction() { Message = x });
                    return Task.CompletedTask;
                }

                if (!int.TryParse(content, out int outValue))
                {
                    return Task.CompletedTask;
                }
                userChoose = (int?) outValue;
                tcs.TrySetResult(new SocketMessageOrReaction() { Message = x });
                return Task.CompletedTask;

            };

            _discord.ReactionAdded += (cache, ch, r) =>
            {
                if (ch.Id != channelId || r.User.Value.IsBot || r.User.Value.IsWebhook)
                {
                    return Task.CompletedTask;
                }

                for (int i = 0; i < optionEmojis.Count; i++)
                {
                    if (optionEmojis[i].Name == r.Emote.Name)
                    {
                        if (isCancellable && i == optionEmojis.Count - 1)
                        {
                            userChoose = int.MaxValue; //Cancel
                        }
                        else
                        {
                            userChoose = i + startNumber;
                        }
                        tcs.TrySetResult(new SocketMessageOrReaction() { Reaction = r });
                        break;
                    }
                }
                return Task.CompletedTask;
            };

            var result = await WaitAsync(tcs, expireAfter);
            if (result == null)
            {
                //逾時未作答，或發生錯誤
                return null;
            }
            else if (result.Message != null || result.Reaction != null)
            {
                return userChoose;
            }
            else
            {
                //Runtime 沒有發生錯誤，卻出現不應該出現的情形: 有收到答案紙，但兩個答案都是空白的。
                return int.MinValue;
            }

            //var msg = await WaitForMessageAsync(
            //    x => 
            //    {
            //        if (x.Channel.Id != channelId || x.Author.IsBot || x.Author.IsWebhook)
            //        {
            //            return false;
            //        }
            //        string content = x.Content.Trim();

            //        if (isCancellable && msgCancelKeywords != null && msgCancelKeywords.Contains(content))
            //        {
            //            userChoose = int.MaxValue;
            //            return true;
            //        }

            //        if (!int.TryParse(content, out userChoose))
            //        {
            //            return false;
            //        }
            //        tcs2.SetCanceled();
            //        return true;
            //    }
            //    , null, tcs1);

            //var rct = await WaitForReactionAsync(
            //    (cache, ch, r) => 
            //    {
            //        if (ch.Id != channelId || r.User.Value.IsBot || r.User.Value.IsWebhook)
            //        {
            //            return false;
            //        }

            //        for (int i = 0; i < optionEmojis.Count; i++)
            //        {
            //            if (optionEmojis[i].Name == r.Emote.Name)
            //            {
            //                if (isCancellable && i == optionEmojis.Count - 1)
            //                {
            //                    userChoose = int.MaxValue; //Cancel
            //                }
            //                else
            //                {
            //                    userChoose = i;
            //                }
            //                tcs1.SetCanceled();
            //                break;
            //            }
            //        }
            //        return false;
            //    }
            //    , null, tcs2);
            //if (msg != null || rct != null)
            //{
            //    return userChoose;
            //}
            //else
            //{
            //    return int.MinValue + 1;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="end">Min is 0</param>
        /// <param name="start">Max is 10</param>
        /// <param name="showCancelButton">Is showing the cancel button</param>
        /// <returns></returns>
        public List<Discord.Emoji> GetNumberOptionsEmojis(int end, int start = 1, bool showCancelButton = true)
        {
            if (start < 0)
            {
                start = 0;
            }
            if (end > 10)
            {
                end = 10;
            }

            var emojiOptionCancel = new Discord.Emoji("❎");
            var emojiNumberOptions = new Discord.Emoji[]
            {
                new Discord.Emoji("0⃣"),
                new Discord.Emoji("1⃣"),
                new Discord.Emoji("2⃣"),
                new Discord.Emoji("3⃣"),
                new Discord.Emoji("4⃣"),
                new Discord.Emoji("5⃣"),
                new Discord.Emoji("6⃣"),
                new Discord.Emoji("7⃣"),
                new Discord.Emoji("8⃣"),
                new Discord.Emoji("9⃣"),
                new Discord.Emoji("🔟"),
            };

            var result = new List<Discord.Emoji>();
            for (int i = start; i <= end; i++)
            {
                result.Add(emojiNumberOptions[i]);
            }
            if (showCancelButton)
            {
                result.Add(emojiOptionCancel);
            }
            return result;
        }

        public class SocketMessageOrReaction
        {
            public SocketMessage Message { get; set; }
            public SocketReaction Reaction { get; set; }
        }
    }

    

    public class ResponsiveOptions
    {
        public ResponsiveOptions()
        {
            DefaultExpireSeconds = 30;
        }

        //[JsonProperty("expire_seconds")]
        public int DefaultExpireSeconds { get; set; }
    }
}
