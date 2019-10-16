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
            LiteDb_MagirecoInfo = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoStat"), null, new Logger(Logger.COMMAND, str=> { Console.WriteLine(str); }));
            //LiteDb_MagirecoInfo.Log.Logging += LiteDb_Logger_Logging;


            LiteCollection_Servers = LiteDb_MagirecoInfo.GetCollection<Server>();
            LiteCollection_Players = LiteDb_MagirecoInfo.GetCollection<Player>();
            LiteCollection_PlayerStats = LiteDb_MagirecoInfo.GetCollection<PlayerStat>();
        }

        //private void LiteDb_Logger_Logging(string obj)
        //{
        //    Console.WriteLine($"[LiteDB_MagirecoStat] {obj}");
        //}

        //public bool AddDailyStats(string server, ushort supportPt, ushort mirrorWin, ushort mirrorLose)
        //{

        //}

        public bool UpsertServer(Server server)
        {
            var existed = LiteCollection_Servers.FindById(server.ServerKey);
            if (existed == null)
            {
                LiteCollection_Servers.Insert(server);
                return true;
            }
            else if (server != existed)
            {
                var result = LiteCollection_Servers.Update(server);
                return result;
            }
            else
            {
                return false;
            }
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
            var existed = LiteCollection_Players.FindById(player.DiscordId);
            if (existed == null)
            {
                LiteCollection_Players.Insert(player);
                return true;
            }
            else if (player != existed)
            {
                var result = LiteCollection_Players.Update(player);
                return result;
            }
            else
            {
                return false;
            }
        }
        public bool DeletePlayer(string playerKey)
        {
            return LiteCollection_Players.Delete(playerKey);
        }
        public IEnumerable<Player> GetPlayers()
        {
            var players = LiteCollection_Players.FindAll();
            return players;
        }
        public Player GetPlayer(string id)
        {
            var player = LiteCollection_Players.FindById(id);
            return player;
        }


        //private void Log_Logging(string msg)
        //{

        //}
    }
}
