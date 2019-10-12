using System;
using System.Collections.Generic;
using System.Text;

namespace Hollen9.NetaSoundIndex.Models
{
    public class CaptionItem
    {
        public string Raw { get; set; }
        public string Culture { get; set; }
        public IDictionary<string, string> Locale { get; set; }
    }
}
