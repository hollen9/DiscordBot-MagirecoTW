using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.Services
{
    public class PlayerInfoService
    {
        private readonly IConfiguration _config;
        
        private LiteDatabase LiteDb_MagirecoPlayerInfo { get; set; }

        public PlayerInfoService(IServiceProvider services)
        {
            _config = services.GetRequiredService<IConfiguration>();
            LiteDb_MagirecoPlayerInfo = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoPlayerInfo"));
        }

        public bool AddDailyStats(ushort SupportPt, ushort MirrorWin, ushort MirrorLose)
        {
            LiteDb_MagirecoPlayerInfo.GetCollection<>
        }

        //private void Log_Logging(string msg)
        //{

        //}
    }
}
