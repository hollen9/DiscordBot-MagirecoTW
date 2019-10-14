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

        public async Task<SocketMessage> WaitForMessageAsync(Func<SocketMessage, bool> condition, TimeSpan? expireAfter = null)
        {
            var tcs = new TaskCompletionSource<SocketMessage>();

            _discord.MessageReceived += (msg) =>
            {
                if (condition(msg))
                    tcs.TrySetResult(msg);
                return Task.CompletedTask;
            };

            return await WaitAsync(tcs, expireAfter);
        }

        public async Task<SocketReaction> WaitForReactionAsync(Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, bool> condition, TimeSpan? expireAfter = null)
        {
            var tcs = new TaskCompletionSource<SocketReaction>();

            _discord.ReactionAdded += (cache, ch, r) =>
            {
                if (condition(cache, ch, r))
                    tcs.TrySetResult(r);
                return Task.CompletedTask;
            };

            return await WaitAsync(tcs, expireAfter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="end">Min is 0</param>
        /// <param name="start">Max is 10</param>
        /// <param name="showCancelButton">Is showing the cancel button</param>
        /// <returns></returns>
        public List<Discord.Emoji> GetNumberOptionsEmojis(byte end, byte start = 1, bool showCancelButton = true)
        {
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
            for (byte i = start; i <= end; i++)
            {
                result.Add(emojiNumberOptions[i]);
            }
            if (showCancelButton)
            {
                result.Add(emojiOptionCancel);
            }
            return result;
        }
    }

    public class ResponsiveOptions
    {
        public ResponsiveOptions()
        {
            DefaultExpireSeconds = 15;
        }

        //[JsonProperty("expire_seconds")]
        public int DefaultExpireSeconds { get; set; }
    }
}
