using System;
using System.Collections.Generic;
using System.Text;

namespace NetaSoundIndex.Models
{
    public class QueryNetaTag : FileNetaTag
    {
        public string Title { get; set; }
        public SourceItem Source { get; set; }
    }
}
