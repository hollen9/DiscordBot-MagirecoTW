using Discord.Commands;
using MitamaBot.DataModels.Magireco;
using MitamaBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MitamaBot.Modules
{
    public class GameInfoModule : MyModuleBase
    {
        public GameInfoService GameInfoSvc { get; set; }
        public ResponsiveService ReponseSvc { get; set; }

        [Command("server")]
        public async Task GetServerInfoAsync()
        {
            var servers = GameInfoSvc.GetServers();
            if (servers == null || servers.Count() <= 0)
            {
                await ReplyAsync("沒有任何伺服器。設定一個吧~");
            }
            else
            {
                var msg = string.Join(",", servers.Select(sv => sv.ServerKey));
                await ReplyAsync(msg);
            }
        }

        [Command("server-delete", RunMode = RunMode.Async)]
        [RequireUserPermission(Discord.ChannelPermission.ManageRoles)]
        public async Task DeleteServerAsync(string serverKey)
        {
            if (GameInfoSvc.DeleteServer(serverKey))
            {
                await ReplyAsync($"已刪除 `{serverKey}`。");
            }
            else
            {
                await ReplyAsync($"不存在 `{serverKey}`。");
            }
        }

        [Command("server-edit", RunMode = RunMode.Async)]
        public async Task EditServerInfoAsync()
        {
            while (true)
            {
                var sv = new Server();
                await ReplyAsync($"請輸入獨特的伺服器辨識碼 (純英文)，例如 `MagirecoJP`");
                sv.ServerKey = (await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                })).Content;
                await ReplyAsync($"請輸入伺服器的中文稱呼，例如 `小圓外傳-日服`");
                sv.ChineseName = (await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                })).Content;
                await ReplyAsync($"請輸入伺服器的介紹，例如 `官方直營。`");
                sv.Description = (await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                })).Content;
                await ReplyAsync($"請輸入伺服器的語言，例如 `jp`, `en`, 或 `zh-Hant`");
                sv.Culture = (await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                })).Content;
                await ReplyAsync($"請輸入開服日期，例如 `2017-08-23`");
                sv.LaunchDate = DateTime.Parse((await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                })).Content);

                var confirmMsg = await ReplyAsync($"確定新增嗎?");
                await confirmMsg.AddReactionAsync(new Discord.Emoji("✅"));
                await confirmMsg.AddReactionAsync(new Discord.Emoji("❎"));

                var ract = await ReponseSvc.WaitForReactionAsync((cache, ch, r) => 
                {
                    return ch.Id == Context.Channel.Id && 
                    (r.Emote.Name == "✅" || r.Emote.Name == "❎");
                });

                if (ract.Emote.Name == "✅")
                {
                    GameInfoSvc.UpsertServer(sv);
                    await ReplyAsync($"已更新: {sv.ServerKey}");
                }
                else
                {
                    await ReplyAsync($"伺服器更新對話結束。");
                }
                await confirmMsg.DeleteAsync();
                return;
            }
        }

        //[Command("account-edit", RunMode = RunMode.Async)]
        //public async Task EditGameAccountAsync()
        //{
        //    var currentPl = GameInfoSvc.GetPlayer(Context.User.Id);
        //    var firstMsgContent_Builder = new StringBuilder();
        //    if (currentPl == null)
        //    {
        //        firstMsgContent_Builder.AppendLine($"歡迎初次使用好友系統～");
        //    }
        //    if (currentPl.ServerKey_PlayerStats == null || currentPl.ServerKey_PlayerStats.Count == 0)
        //    {
        //        firstMsgContent_Builder.Append($"你還沒有綁定任何遊戲帳號，");
        //    }
        //    firstMsgContent_Builder.Append($"請問你想要綁定的帳號，隸屬於哪一個伺服器？");
        //    var embedPromptServer_Builder = new Discord.EmbedBuilder();
        //    embedPromptServer_Builder.Title = "請選擇伺服器";
        //    embedPromptServer_Builder.Description = "";

        //    var firstMsg = ReplyAsync(firstMsgContent_Builder, false, )


        //}

        [Command("profile-edit", RunMode = RunMode.Async)]
        public async Task EditPlayerDescription()
        {
            var currentPl = GameInfoSvc.GetPlayer(Context.User.Id);
            if (currentPl == null)
            {
                await ReplyAsync($"歡迎初次使用好友系統～");
            }
            else if (string.IsNullOrEmpty(currentPl.Description))
            {
                await ReplyAsync($"尚未填寫個人簡介。");
            }
            else
            {
                await ReplyAsync($"目前的個人簡介為:\n");
            }

            while (true)
            {
                var pl = new Player();
                pl.DiscordId = Context.User.Id;

                var promptMsg = await ReplyAsync($"請輸入新的個人簡介。**(字數50字以內。)**");

                var userAnsMsg = await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return msg.Channel.Id == Context.Channel.Id;
                });

                if (userAnsMsg == null)
                {
                    await promptMsg.ModifyAsync(x=> 
                    {
                        x.Content = "> 操作逾時。";
                    });
                    return;
                }

                if (userAnsMsg.Content.Length > 50)
                {
                    await ReplyAsync($"字數不得超過50字。\n請輸入個人簡介。");
                    continue;
                }

                var confirmMsg = await ReplyAsync($"確定嗎?");
                await confirmMsg.AddReactionAsync(new Discord.Emoji("✅"));
                await confirmMsg.AddReactionAsync(new Discord.Emoji("❎"));

                var ract = await ReponseSvc.WaitForReactionAsync((cache, ch, r) =>
                {
                    return ch.Id == Context.Channel.Id &&
                    (r.Emote.Name == "✅" || r.Emote.Name == "❎");
                });

                if (ract.Emote.Name == "✅")
                {
                    GameInfoSvc.UpsertPlayer(pl);
                    await ReplyAsync($"個人簡介更新成功。");
                }
                else
                {
                    await ReplyAsync($"已放棄變更個人簡介。");
                }
                await confirmMsg.DeleteAsync();
                return;
            }
        }

        [Command("menu", RunMode = RunMode.Async)]
        public async Task ShowGameMenuAsync()
        {
            var embedBuilder = new Discord.EmbedBuilder();
            embedBuilder.Title = "Magireco選單";
            embedBuilder.Description =
                $"1. 編輯個人檔案\n" +
                $"2. 編輯遊戲ID\n" +
                $"3. 尋找尚未 Follow 的人\n" +
                $"4. 被我 Following 的人\n" +
                $"5. 我的 Followers\n" +
                $"6. 相互追隨的人"; ;

            var msgEntryPoint = await ReplyEmbedAsync(embedBuilder);

            //var emojiNumberOptions = new Discord.Emoji[] 
            //{
            //    null,
            //    new Discord.Emoji("1⃣"),
            //    new Discord.Emoji("2⃣"),
            //    new Discord.Emoji("3⃣"),
            //    new Discord.Emoji("4⃣"),
            //    new Discord.Emoji("5⃣"),
            //    new Discord.Emoji("6⃣"),
            //};
            //var emojiOptionCancel = new Discord.Emoji("❎");

            var reactButtons = ReponseSvc.GetNumberOptionsEmojis(6);


            for (int i = 0; i < 6 + 1; i++)
            {
                await msgEntryPoint.AddReactionAsync(reactButtons[i]);
            }
            
            int userChoice = -1;
            bool isCancel = false;
            var ract = await ReponseSvc.WaitForReactionAsync((cache, ch, r) =>
            {
                if (r.User.Value.IsBot)
                {
                    return false;
                }

                var uMoji = r.Emote as Discord.Emoji;

                if (uMoji.Name == reactButtons.Last().Name)
                {
                    userChoice = int.MinValue;
                    return true;
                }

                userChoice = reactButtons.IndexOf(uMoji) + 1;
                if (userChoice >= 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

            

            Discord.Embed secondEmbed;
            string secondContent;

            if (isCancel)
            {
                
            }
            
            switch (userChoice)
            {
                case int.MinValue:
                    secondEmbed = null;
                    secondContent = $"> {Context.User.Mention} 已取消。";
                    break;
                default:
                case -1:
                    secondEmbed = null;
                    secondContent = $"> {Context.User.Mention} 操作逾時。";
                    break;
                case 1:
                    await EditPlayerDescription();
                    await msgEntryPoint.DeleteAsync();
                    return;
            }
            await msgEntryPoint.RemoveAllReactionsAsync();
            await msgEntryPoint.ModifyAsync(x => {
                x.Content = secondContent;
                x.Embed = secondEmbed;
            });
            
        }
    }
}
