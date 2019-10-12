using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MitamaBot.Services;

namespace MitamaBot.DataModels.Magireco
{
    public class Player
    {
        public long DiscordId { get; set; }
        public string Description { get; set; }
        public string FriendPolicy { get; set; }
        public Dictionary<string, PlayerStat> ServerKey_PlayerStats { get; set; }
    }
}
