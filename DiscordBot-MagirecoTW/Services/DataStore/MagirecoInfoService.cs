using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Microsoft.Extensions.Configuration;
using MitamaBot.DataModels.Magireco;

namespace MitamaBot.Services.DataStore
{
    public class MagirecoInfoService
    {
        private IConfiguration _config;
        private LiteDatabase Database { get; }

        public IDataStore<Player, BsonValue> Player { get; set; }
        public IDataStore<PlayerAccount, BsonValue> PlayerAccount { get; }
        public IDataStore<Server, BsonValue> Server { get; }
        public IDataStore<PlayerDailyStat, BsonValue> PlayerDailyStat { get; set; }


        public MagirecoInfoService(IConfiguration config)
        {
            _config = config;
            Database = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoStat"), null, new Logger(Logger.COMMAND, str => { Console.WriteLine(str); }));
            Player = new PlayerCollectionWrapper(Database);
            PlayerAccount = new PlayerAccountCollectionWrapper(Database);
            Server = new ServerCollectionWrapper(Database);
        }

        public class PlayerCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.Player>
        {
            public PlayerCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
        public class PlayerAccountCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.PlayerAccount>
        {
            public PlayerAccountCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
        public class ServerCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.Server>
        {
            public ServerCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
        public class PlayerDailyStatCollectionWrapper : LiteDbCollectionWrapper<DataModels.Magireco.PlayerDailyStat>
        {
            public PlayerDailyStatCollectionWrapper(LiteDatabase database) : base(database)
            { }
        }
    }
}
