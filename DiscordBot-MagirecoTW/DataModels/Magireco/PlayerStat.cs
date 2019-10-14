using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MitamaBot.Services;

namespace MitamaBot.DataModels.Magireco
{
    public class PlayerStat
    {
        [LiteDB.BsonId(true)]
        public Guid Id { get; set; }
        public string GameId { get; set; }
        public string GameHandle { get; set; }
        public string Description { get; set; }
        public List<long> Following { get; set; }
        public List<long> Follower { get; set; }
    }
}
