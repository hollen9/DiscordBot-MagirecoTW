using System;
using System.Collections.Generic;
using System.Text;

namespace Hollen9.NetaSoundIndex.Models
{
    public partial class QueryNetaTag : FileNetaTag
    {
        public string Title { get; set; }
        public SourceItem Source { get; set; }
    }

    public partial class QueryNetaTag
    {
        public void DeepCopy(FileNetaTag netaItem)
        {
            SourceGuid = netaItem.SourceGuid;
            Characters = netaItem.Characters;
            AuthorsDiscordId = netaItem.AuthorsDiscordId;
            Alias = netaItem.Alias;
            Filename = netaItem.Filename;
        }
    }
}
