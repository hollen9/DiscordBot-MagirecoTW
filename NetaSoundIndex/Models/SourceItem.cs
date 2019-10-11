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

    /* Example Json:
      {
        "e4d592eb4a1f402f8024ee6838b50eea": {
          "Title": "TVアニメ「マギアレコード 魔法少女まどか☆マギカ外伝」予告CM『マギレポ劇場』",
          "Urls": [
            "https://www.youtube.com/playlist?list=PLce8zkkVzJrGfgJRJ9lEcEN6DnDFI3QxL"
          ]
        }
      } 
     */
}
