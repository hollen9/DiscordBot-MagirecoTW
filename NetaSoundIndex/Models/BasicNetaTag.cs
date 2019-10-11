using System;
using System.Collections.Generic;
using System.Text;

namespace NetaSoundIndex.Models
{
    public class BasicNetaTag
    {
        public string[] Characters { get; set; }
        public Guid SourceGuid { get; set; }
        public string[] Alias { get; set; }
        public long[] AuthorsDiscordId { get; set; }
    }
}
