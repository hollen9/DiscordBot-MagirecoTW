using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MitamaBot.Helpers
{
    public static class DiscordEmbedHelper
    {
        public static Discord.Embed BuildLinesOfOptions(string title, List<string> options, int start = 1, string[] cancelKeywords = null)
        {
            var eB = new Discord.EmbedBuilder();
            eB.Title = title;
            var sB = new StringBuilder();
            for (int i = start; i <= options.Count; i++)
            {
                if (start == 0) //zero-based options
                {
                    if (i == options.Count)
                    {
                        break;
                    }
                    sB.AppendLine($"{i}. {options[i]}");
                }
                else
                {
                    sB.AppendLine($"{i}. {options[i - 1]}");
                }
            }
            if (cancelKeywords != null)
            {
                eB.WithFooter($"取消指令: {string.Join("、", cancelKeywords)} ");
            }
            eB.Description = sB.ToString();
            return eB.Build();
        }

        //public static Discord.Embed BuildLinesOfOptions(string title, string[] cancelKeywords = null, int start = 1, params string[] options)
        //{
        //    return BuildLinesOfOptions(title, cancelKeywords.ToList(), start, cancelKeywords);
        //}
    }
}
