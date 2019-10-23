using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.Helper
{
    public static class MessageHelper
    {
        public enum ResultKind
        {
            Succeed,
            Default,
            Failed,
            Warning,
            Forbidden
        }

        public static string ContentWithMention(this Discord.IUser user, string content, ResultKind rmk = ResultKind.Default)
        {
            string middle;
            switch (rmk)
            {
                default:
                case ResultKind.Default:
                    middle = " ";
                    break;
                case ResultKind.Succeed:
                    middle = " ✅";
                    break;
                case ResultKind.Failed:
                    middle = " 😦";
                    break;
                case ResultKind.Warning:
                    middle = " ⚠";
                    break;
                case ResultKind.Forbidden:
                    middle = " 🚫";
                    break;
            }

            return user.Mention + middle + content;
        }
    }
}
