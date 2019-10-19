using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MitamaBot.Helpers
{
    public static class DiscordEmbedHelper
    {
        public static Discord.Embed BuildLinesOfOptions(string title, List<string> options, string[] cancelKeywords = null)
        {
            var eB = new Discord.EmbedBuilder();
            eB.Title = title;
            var sB = new StringBuilder();
            for (int i = 0; i < options.Count; i++)
            {
                sB.AppendLine($"{i+1}. {options[i]}");
            }
            if (cancelKeywords != null)
            {
                eB.WithFooter($"取消指令: {string.Join("、", cancelKeywords)} ");
            }
            eB.Description = sB.ToString();
            return eB.Build();
        }

        public static Discord.Embed BuildLinesOfOptions(string title, string[] cancelKeywords = null, params string[] options)
        {
            return BuildLinesOfOptions(title, cancelKeywords.ToList(), cancelKeywords);
        }
    }
}
