using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MitamaBot.DataModels.Magireco;

namespace MitamaBot.Services
{
    public class GameInfoService
    {
        private readonly IConfiguration _config;
        
        private LiteDatabase LiteDb_MagirecoInfo { get; set; }
        private LiteCollection<Server> LiteCollection_Servers { get; set; }
        private LiteCollection<Player> LiteCollection_Players { get; set; }
        private LiteCollection<PlayerStat> LiteCollection_PlayerStats { get; set; }

        public GameInfoService(IConfiguration config)
        {
            _config = config;
            LiteDb_MagirecoInfo = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoStat"));

            LiteCollection_Servers = LiteDb_MagirecoInfo.GetCollection<Server>();
            LiteCollection_Players = LiteDb_MagirecoInfo.GetCollection<Player>();
            LiteCollection_PlayerStats = LiteDb_MagirecoInfo.GetCollection<PlayerStat>();
        }

        //public bool AddDailyStats(string server, ushort supportPt, ushort mirrorWin, ushort mirrorLose)
        //{

        //}

        public bool UpsertServer(Server server)
        {
            return LiteCollection_Servers.Upsert(server);
        }
        public bool DeleteServer(string serverKey)
        {
            return LiteCollection_Servers.Delete(serverKey);
        }
        public IEnumerable<Server> GetServers()
        {
            return LiteCollection_Servers.FindAll();
        }
        public bool UpsertPlayer(Player player)
        {
            return LiteCollection_Players.Upsert(player);
        }
        public bool DeletePlayer(long playerKey)
        {
            return LiteCollection_Players.Delete(playerKey);
        }
        public IEnumerable<Player> GetPlayers()
        {
            return LiteCollection_Players.FindAll();
        }
        public Player GetPlayer(ulong id)
        {
            return LiteCollection_Players.FindById(id);
        }


        //private void Log_Logging(string msg)
        //{

        //}
    }
}
