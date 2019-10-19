using System;

namespace MitamaBot.DataModels.Magireco
{
    public class PlayerDailyStat
    {
        public Guid Id { get; set; }
        public string ServerKey { get; set; }
        public Guid PlayerAccountId { get; set; }
        public DateTime Date { get; set; }
        public ushort EarnSupportScore { get; set; }
        public ushort MirrorDefenseWin { get; set; }
        public ushort MirrorDefenseLose { get; set; }
    }
}