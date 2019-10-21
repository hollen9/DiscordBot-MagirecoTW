using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.DataModels.Magireco
{
    public class FollowingInfo
    {
        [LiteDB.BsonId(true)]
        public Guid Id { get; set; }
        public Guid MyAccountId { get; set; }
        public Guid FollowingAccountId { get; set; }
    }
}
