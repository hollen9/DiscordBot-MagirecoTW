using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MitamaBot.Services;

namespace MitamaBot.DataModels.Magireco
{
    public partial class Player
    {
        [LiteDB.BsonId(false)]
        public string DiscordId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, PlayerStat> ServerKey_PlayerStats { get; set; }
    }

    [LiteDB.BsonIgnore]
    public partial class Player
    {
        public Player() { }

        [LiteDB.BsonIgnore]
        public Player(string discordId)
        {
            DiscordId = discordId;

            Description = string.Empty;
            ServerKey_PlayerStats = new Dictionary<string, PlayerStat>();
        }
    }
}
