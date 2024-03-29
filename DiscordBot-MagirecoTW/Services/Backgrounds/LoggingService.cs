﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MitamaBot.Services.Backgrounds
{
    public class LoggingService
    {
        private readonly ILoggerFactory _factory;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        public LoggingService(
            ILoggerFactory factory,
            DiscordSocketClient discord,
            CommandService commands)
        {
            _factory = factory;
            _commands = commands;
            _discord = discord;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            var logger = _factory.CreateLogger("Discord." + msg.Source);
            string message = msg.Exception?.ToString() ?? msg.Message;
            switch (msg.Severity)
            {
                case LogSeverity.Debug:
                    logger.LogDebug(message);
                    break;
                case LogSeverity.Warning:
                    logger.LogWarning(message);
                    break;
                case LogSeverity.Error:
                    logger.LogError(message);
                    break;
                case LogSeverity.Critical:
                    logger.LogCritical(message);
                    break;
                default:
                    logger.LogInformation(message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
