using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly DiscordSocketClient _discord;

        public ResponsiveOptions Options { get; }

        public Emoji CancelEmoji { get; }
        public Emoji YesEmoji { get; }
        public Emoji NoEmoji { get; }

        public ResponsiveService(/*ILogger<ResponsiveService> logger, */IConfiguration config, DiscordSocketClient discord)
        {
            //_logger = logger;
            _discord = discord;
            
            var options = new ResponsiveOptions();
            config.Bind("ResponsiveOptions", options);
            Options = options;

            CancelEmoji = new Emoji(Options.CancelEmojiUnicode);
            YesEmoji = new Emoji(Options.YesEmojiUnicode);
            NoEmoji = new Emoji(Options.NoEmojiUnicode);
        }

        private async Task<T> WaitAsync<T>(TaskCompletionSource<T> tcs, TimeSpan? expireAfter = null)
        {
            new Timer((s) => tcs.TrySetCanceled(), null, expireAfter == null ? TimeSpan.FromSeconds(Options.DefaultExpireSeconds) : (TimeSpan)expireAfter, TimeSpan.Zero);
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
        /// <para>取消回傳 null。</para>
        /// <para>逾時未答拋出 TimeoutException。</para>
        /// <para>不明原因拋出 Exception。</para>
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
            TimeSpan? expireAfter = null,
            TaskCompletionSource<SocketMessageOrReaction> tcs = null)
        {
            if (tcs == null)
            {
                tcs = new TaskCompletionSource<SocketMessageOrReaction>();
            }

            int endNumber;
            if (startNumber == 0)
            {
                if (isCancellable)
                {
                    endNumber = optionEmojis.Count - 2;
                }
                else
                {
                    endNumber = optionEmojis.Count - 1;
                }
            }
            else
            {
                if (isCancellable)
                {
                    endNumber = optionEmojis.Count + startNumber - 2;
                }
                else
                {
                    endNumber = optionEmojis.Count + startNumber - 1;
                }
            }
            
            int? userChoose = null;

            _discord.MessageReceived += (x) =>
            {
                if (x.Channel.Id != channelId || x.Author.IsBot || x.Author.IsWebhook)
                {
                    return Task.CompletedTask;
                }
                string content = x.Content.Trim();

                if (isCancellable && Options.CancelKeywords != null && Options.CancelKeywords.Contains(content))
                {
                    userChoose = null;
                    tcs.TrySetResult(new SocketMessageOrReaction() { Message = x });
                    return Task.CompletedTask;
                }

                if (!int.TryParse(content, out int outValue) || outValue < startNumber || outValue > endNumber)
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
                            userChoose = null; //Cancel
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
                throw new TimeoutException("逾時未作答");
            }
            else if (result.Message != null || result.Reaction != null)
            {
                return userChoose;
            }
            else
            {
                throw new Exception("Runtime 沒有發生錯誤，卻出現不應該出現的情形: 有收到答案紙，但兩個答案都是空白的。");
            }
        }

        public async Task<bool?> WaitForBooleanAnswerAsync(
            ulong channelId,
            bool isCancellable = false,
            TimeSpan? expireAfter = null,
            TaskCompletionSource<SocketMessageOrReaction> tcs = null)
        {
            if (tcs == null)
            {
                tcs = new TaskCompletionSource<SocketMessageOrReaction>();
            }

            bool? userChoose = null;

            _discord.MessageReceived += (x) =>
            {
                if (x.Channel.Id != channelId || x.Author.IsBot || x.Author.IsWebhook)
                {
                    return Task.CompletedTask;
                }
                string content = x.Content.Trim().ToLower();

                if (isCancellable && Options.CancelKeywords != null && Options.CancelKeywords.Contains(content))
                {
                    userChoose = null;
                    tcs.TrySetResult(new SocketMessageOrReaction() { Message = x });
                    return Task.CompletedTask;
                }
                bool hasYesStr = Options.YesKeywords.Contains(content),
                     hasNoStr = Options.NoKeywords.Contains(content);

                if (!hasYesStr && !hasNoStr)
                {
                    return Task.CompletedTask;
                }
                if (hasYesStr)
                {
                    userChoose = true;
                }
                else if (hasNoStr)
                {
                    userChoose = false;
                }
                else
                {
                    return Task.CompletedTask;
                    // Ignore and abandon this value
                }

                tcs.TrySetResult(new SocketMessageOrReaction() { Message = x });
                return Task.CompletedTask;

            };

            _discord.ReactionAdded += (cache, ch, r) =>
            {
                if (ch.Id != channelId || r.User.Value.IsBot || r.User.Value.IsWebhook)
                {
                    return Task.CompletedTask;
                }

                if (isCancellable && r.Emote.Name == CancelEmoji.Name)
                {
                    userChoose = null;
                }
                else if (r.Emote.Name == YesEmoji.Name)
                {
                    userChoose = true;
                }
                else if (r.Emote.Name == NoEmoji.Name)
                {
                    userChoose = false;
                }
                else
                {
                    //Ignore as not setting task result;-
                    return Task.CompletedTask;
                }
                tcs.TrySetResult(new SocketMessageOrReaction() { Reaction = r });
                return Task.CompletedTask;
            };

            var result = await WaitAsync(tcs, expireAfter);
            if (result == null)
            {
                //逾時未作答，或發生錯誤
                throw new TimeoutException("逾時未作答");
            }
            else if (result.Message != null || result.Reaction != null)
            {
                return userChoose;
            }
            else
            {
                throw new Exception("Runtime 沒有發生錯誤，卻出現不應該出現的情形: 有收到答案紙，但兩個答案都是空白的。");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="end">Min is 0</param>
        /// <param name="start">Max is 10</param>
        /// <param name="showCancelButton">Is showing the cancel button</param>
        /// <returns></returns>
        public List<Emoji> GetNumberOptionsEmojis(int end, int start = 1, bool showCancelButton = true)
        {
            if (start < 0)
            {
                start = 0;
            }
            if (end > 10)
            {
                end = 10;
            }

            var emojiOptionCancel = CancelEmoji;
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
                if (start == 0 && i == end)
                {
                    break;
                }
                result.Add(emojiNumberOptions[i]);
            }
            if (showCancelButton)
            {
                result.Add(emojiOptionCancel);
            }
            return result;
        }

        public List<Emoji> GetBooleanOptionsEmojis(bool showCancelButton = true)
        {
            var emojis = new List<Emoji>();
            emojis.Add(YesEmoji);
            emojis.Add(NoEmoji);
            if (showCancelButton)
            {
                emojis.Add(CancelEmoji);
            }
            return emojis;
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
        public int DefaultExpireSeconds { get; set; }
        public string[] YesKeywords { get; set; }
        public string[] NoKeywords { get; set; }
        public string[] CancelKeywords { get; set; }
        public string CancelEmojiUnicode { get; set; }
        public string YesEmojiUnicode { get; set; }
        public string NoEmojiUnicode { get; set; }
    }
}
