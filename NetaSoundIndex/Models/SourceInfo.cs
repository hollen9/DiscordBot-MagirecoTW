using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetaSoundIndex.Models
{
    public class SourceItem
    {
        public string Title { get; set; }
        public List<string> Urls { get; set; }
    }

    //public class SourceInfo
    //{
    //    public Dictionary<Guid, SourceItem> SourceItems { get; set; }
    //}
}
