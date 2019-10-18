using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace MitamaBot.Services.DataStore
{
    public class MagirecoInfoService
    {
        private IConfiguration _config;
        public LiteDatabase Database { get; set; }
        public PlayerCollectionWrapper PlayerCW { get; set; }
        public PlayerStatCollectionWrapper PlayerStatCW { get; set; }
        public ServerCollectionWrapper ServerCW { get; set; }


        public MagirecoInfoService(IConfiguration config)
        {
            _config = config;
            Database = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoStat"), null, new Logger(Logger.COMMAND, str => { Console.WriteLine(str); }));
            PlayerCW = new PlayerCollectionWrapper(Database);
            PlayerStatCW = new PlayerStatCollectionWrapper(Database);
            ServerCW = new ServerCollectionWrapper(Database);
        }

        public class PlayerCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.Player>
        {
            public PlayerCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
        public class PlayerStatCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.PlayerStat>
        {
            public PlayerStatCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
        public class ServerCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.Server>
        {
            public ServerCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
    }
}
