using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.DataModels.Magireco
{
    public class Server
    {
        public string ServerKey { get; set; } //CHTServer
        public string ChineseName { get; set; }
        public string Description { get; set; }
        public string Culture { get; set; } //zh-Hant
        public DateTime LaunchDate { get; set; }

        /// <summary>
        /// Discord
        /// </summary>
        public List<long> Players { get; set; }
    }
}
