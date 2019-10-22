using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MitamaBot.Services;

namespace MitamaBot.DataModels.Magireco
{
    /// <summary>
    /// 玩家的帳號數據
    /// </summary>
    public class PlayerAccount
    {
        [LiteDB.BsonId(true)]
        public Guid Id { get; set; }
        public string GameId { get; set; }
        public string GameHandle { get; set; }
        public string Description { get; set; }
        public string ProfileImageUrl { get; set; }
        public string OwnerDiscordId { get; set; }
        public string OwnerServerKey { get; set; }
    }
}
