using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;
using Microsoft.Extensions.Configuration;
using MitamaBot.DataModels.Magireco;

namespace MitamaBot.Services.DataStore
{
    public class MagirecoInfoService
    {
        private IConfiguration _config;
        private LiteDatabase Database { get; }

        /// <summary>
        /// PK: Discord ID (string)
        /// </summary>
        public IDataStore<Player, BsonValue> Player { get; set; }
        public PlayerAccountCollectionWrapper PlayerAccount { get; }
        public IDataStore<Server, BsonValue> Server { get; }
        public IDataStore<PlayerDailyStat, BsonValue> PlayerDailyStat { get; set; }
        public FollowingInfoCollectionWrapper FollowingInfo { get; set; }


        public MagirecoInfoService(IConfiguration config)
        {
            _config = config;
            Database = new LiteDatabase(_config.GetConnectionString("LiteDb_MagirecoStat"), null, new Logger(Logger.COMMAND, str => { Console.WriteLine(str); }));
            Player = new PlayerCollectionWrapper(Database);
            PlayerAccount = new PlayerAccountCollectionWrapper(Database);
            Server = new ServerCollectionWrapper(Database);
            FollowingInfo = new FollowingInfoCollectionWrapper(Database);
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

            public override bool UpdateItem(PlayerAccount item, BsonValue id)
            {
                item.LastUpdateTimestamp = DateTime.Now;
                return base.UpdateItem(item, id);
            }

            public override bool UpsertItem(PlayerAccount item, BsonValue id)
            {
                item.LastUpdateTimestamp = DateTime.Now;
                return base.UpsertItem(item, id);
            }

            public override BsonValue AddItem(PlayerAccount item)
            {
                item.LastUpdateTimestamp = DateTime.Now;
                return base.AddItem(item);
            }

            /// <summary>
            /// Get a Discord User's account data Group by Server key
            /// </summary>
            /// <param name="discordId"></param>
            /// <returns></returns>
            public Dictionary<string, List<PlayerAccount>> FindAccountsByIdAndGroupByServer(ulong discordId)
            {
                return FindItems(pa =>
                    pa.OwnerDiscordId == discordId.ToString())
                    .GroupBy(x => x.OwnerServerKey)
                    .ToDictionary(x => x.Key, x => x.ToList());
            }
            public List<PlayerAccount> FindAccountsByIdAndServer(ulong discordId, string serverKey)
            {
                return FindItems(pa =>
                    pa.OwnerDiscordId == discordId.ToString() &&
                    pa.OwnerServerKey == serverKey).ToList();
            }
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
        public class FollowingInfoCollectionWrapper : LiteDbCollectionWrapper<FollowingInfo>
        {
            public FollowingInfoCollectionWrapper(LiteDatabase database) : base(database)
            { }
            public IEnumerable<FollowingInfo> FindMyFollowingAccount(Guid myAccountId)
            {
                return this.FindItems(x => x.MyAccountId == myAccountId);
            }
            public IEnumerable<FollowingInfo> FindMyFollowerAccount(Guid myAccountId)
            {
                return this.FindItems(x => x.FollowingAccountId == myAccountId);
            }
        }
    }
}
