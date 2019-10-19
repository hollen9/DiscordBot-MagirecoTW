using Discord;
using Discord.Commands;
using MitamaBot.DataModels.Magireco;
using MitamaBot.Services;
using MitamaBot.Services.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MitamaBot.Modules
{
    public class GameInfoModule : MyModuleBase
    {
        //public GameInfoService GameInfoSvc { get; set; }
        public MagirecoInfoService MagirecoInfoSvc { get; set; }
        public ResponsiveService ReponseSvc { get; set; }

        [Command("test-opt", RunMode = RunMode.Async)]
        public async Task TestOptionsAsync()
        {
            bool isCancellable = true;
            var emojis = ReponseSvc.GetNumberOptionsEmojis(10, 1, isCancellable);

            var msg = await ReplyAsync("請選擇");
            //Fire-and-forget
            CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
            await Task.Factory.StartNew(async () => await msg.AddReactionsAsync(emojis.ToArray(), new RequestOptions() {CancelToken = tcs_react_adding.Token}));
            int? userChoice = await ReponseSvc.WaitForNumberAnswerAsync(Context.Channel.Id, emojis, 1, isCancellable, new[] {"esc"}, TimeSpan.FromSeconds(5));
            tcs_react_adding.Cancel();

            await msg.RemoveAllReactionsAsync();
            await msg.ModifyAsync(x=> 
            {
                x.Content = $"選擇 {userChoice ?? -9999}";
            });
        }

        [Command("server")]
        public async Task GetServerInfoAsync()
        {            
            var servers = MagirecoInfoSvc.Server.GetItems();
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
            if (MagirecoInfoSvc.Server.DeleteItem(serverKey))
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
                    MagirecoInfoSvc.Server.UpsertItem(sv, sv.ServerKey);
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

        [Command("account-edit", RunMode = RunMode.Async)]
        public async Task EditGameAccountAsync()
        {
            var playerDiscordId = Context.User.Id.ToString();

            var servers = MagirecoInfoSvc.Server.GetItems().ToList();
            if (servers == null || servers.Count == 0)
            {
                await ReplyMentionAsync("噢噢! 好像還沒有設定任何伺服器ㄛ\n先等管理員新增完成了再來使用ㄅ");
                return;
            }

            var currentPl = MagirecoInfoSvc.Player.GetItem(Context.User.Id.ToString());
            var firstMsgContent_Builder = new StringBuilder();
            if (currentPl == null)
            {
                var noPlWarnMsg = await ReplyAsync("由於你還沒有建置玩家檔案，所以先引導建立好，再回頭綁定ID吧~");
                await EditPlayerDescription();
                currentPl = MagirecoInfoSvc.Player.GetItem(Context.User.Id.ToString());

                if (currentPl == null)
                {
                    await noPlWarnMsg.ModifyAsync(msg=>
                    {
                        msg.Content = $"{Context.User.Mention} :warning: 資料存取錯誤: 個人檔案建立失敗，中止帳號綁定程序。";
                    });
                    return;
                }
            }

            var playerAccounts = MagirecoInfoSvc.PlayerAccount.FindItems(pAccount => pAccount.OwnerDiscordId == playerDiscordId).ToList();

            if (playerAccounts == null || playerAccounts.Count == 0)
            {
                firstMsgContent_Builder.Append($"你還沒有綁定任何遊戲帳號，");
            }
            firstMsgContent_Builder.Append($"請問你想要查看哪個伺服器？");

            var embedPromptServer_Builder = new Discord.EmbedBuilder();
            embedPromptServer_Builder.Title = "遊戲帳號伺服器";

            var embedPromptServerDescription_Builder = new StringBuilder();
            for (int i = 0; i < servers.Count; i++)
            {
                embedPromptServerDescription_Builder.AppendLine($"{i + 1}. {servers[i].ChineseName} `{servers[i].ServerKey}`");
            }
            List<Discord.Emoji> reactButtons = null;

            if (servers.Count <= 10 && servers.Count > 0)
            {
                reactButtons = ReponseSvc.GetNumberOptionsEmojis((byte)(servers.Count));
                
            }

            embedPromptServer_Builder.Description = embedPromptServerDescription_Builder.ToString();

            var msgEntryPoint = await ReplyAsync(firstMsgContent_Builder.ToString(), false, embedPromptServer_Builder.Build());



            int userChoice = -1;

            if (reactButtons != null)
            {
                reactButtons.ForEach(async emoji =>
                {
                    await msgEntryPoint.AddReactionAsync(emoji);
                });

                var ract = await ReponseSvc.WaitForReactionAsync((cache, ch, r) =>
                {
                    if (r.User.Value.IsBot || r.UserId != Context.User.Id)
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
            }
            else
            {
                while (true)
                {
                    var userAnsMsg = await ReponseSvc.WaitForMessageAsync((msg) =>
                        {
                            if (msg.Author.IsBot || msg.Author.Id != Context.User.Id)
                            {
                                return false;
                            }
                            return msg.Channel.Id == Context.Channel.Id;
                        });
                    if (!int.TryParse(userAnsMsg.Content, out userChoice) || userChoice <= 0 || userChoice > servers.Count)
                    {
                        await ReplyMentionAsync("請輸入有效選項!");
                        continue;
                    }
                    break;
                }
            }

            //完成詢問目標伺服器
            Server serverInfo = null;
            
            Discord.Embed preEmbed = null;
            string preContent = null;
            if (userChoice == int.MinValue)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} 已取消。";
            }
            else if (userChoice == -1)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} 操作逾時。";
            }
            else if (userChoice -1 >= 0 && userChoice <= servers.Count)
            {
                serverInfo = servers[userChoice - 1];

                string embedDescription = "沒有任何帳號，。";

                preEmbed = new EmbedBuilder {
                    Title = $"編輯 {serverInfo.ChineseName} 的帳號，或新增一個",
                    Description = embedDescription
                }.Build();
                preContent = null;
            }

            await msgEntryPoint.RemoveAllReactionsAsync();
            await msgEntryPoint.ModifyAsync(x => {
                x.Content = preContent;
                x.Embed = preEmbed;
            });

            if (serverInfo == null)
            {
                return;
            }



            //string playerId;

            //while (true)
            //{
            //    var userAnsMsg = await ReponseSvc.WaitForMessageAsync((msg) =>
            //    {
            //        if (msg.Author.IsBot || msg.Author.Id != Context.User.Id)
            //        {
            //            return false;
            //        }

            //        return (msg.Channel.Id == Context.Channel.Id);
            //    });
            //    if (userAnsMsg.Content.Length != 8)
            //    {
            //        await msgEntryPoint.ModifyAsync(x => 
            //        {
            //            x.Content = secondContent + "\n　:warning: **輸入錯誤! 好友ID為8碼。**";
            //        });
            //        continue;
            //    }
            //    playerId = userAnsMsg.Content;
            //    break;
            //}




            
            //var confirmMsg = await ReplyMentionAsync($"綁定 __`{playerId}`__ 到 **`{serverName}`**，是嗎？");
            //await confirmMsg.AddReactionAsync(new Discord.Emoji("✅"));
            //await confirmMsg.AddReactionAsync(new Discord.Emoji("❎"));

            //var ractConfirm = await ReponseSvc.WaitForReactionAsync((cache, ch, r) =>
            //{
            //    if (r.User.Value.IsBot || r.UserId != Context.User.Id)
            //    {
            //        return false;
            //    }

            //    return ch.Id == Context.Channel.Id &&
            //    (r.Emote.Name == "✅" || r.Emote.Name == "❎");
            //});

            //if (ractConfirm == null)
            //{
            //    await msgEntryPoint.DeleteAsync();
            //    await confirmMsg.ModifyAsync(x =>
            //    {
            //        x.Content = $"{Context.User.Mention} 操作逾時。";
            //        x.Embed = null;
            //    });
            //    await confirmMsg.RemoveAllReactionsAsync();
            //}
            //else if (ractConfirm.Emote.Name == "✅")
            //{

            //    if (currentPl.ServerKey_PlayerAccounts.TryAdd(serverKey, new PlayerAccount()
            //    {
            //        GameId = playerId,
            //        Id = Guid.NewGuid(),
            //    }))
            //    {
            //        var existPStat = currentPl.ServerKey_PlayerAccounts[serverKey];
            //        existPStat.GameId = playerId;
            //    }
            //    var isSucceed = MagirecoInfoSvc.PlayerCW.UpsertItem(currentPl, currentPl.DiscordId);

            //    if (isSucceed)
            //    {
            //        await confirmMsg.ModifyAsync(x =>
            //        {
            //            x.Content = $"{Context.User.Mention} 已新增 __`{playerId}`__ 至 **`{serverName}`**。";
            //            x.Embed = null;
            //        });
            //    }
            //    else
            //    {
            //        await confirmMsg.ModifyAsync(x =>
            //        {
            //            x.Content = $"{Context.User.Mention} :warning: 失敗: 無法新增 __`{playerId}`__ 至 **`{serverName}`**。";
            //            x.Embed = null;
            //        });
            //    }
            //    await msgEntryPoint.DeleteAsync();
            //    await confirmMsg.RemoveAllReactionsAsync();
            //}
            //else
            //{
            //    await msgEntryPoint.DeleteAsync();
            //    await confirmMsg.ModifyAsync(x =>
            //    {
            //        x.Content = $"{Context.User.Mention} 已放棄綁定小圓ID程序。";
            //        x.Embed = null;
            //    });
            //    await confirmMsg.RemoveAllReactionsAsync();
            //}
        }

        [Command("profile-edit", RunMode = RunMode.Async)]
        public async Task EditPlayerDescription()
        {
            var currentPl = MagirecoInfoSvc.Player.GetItem(Context.User.Id.ToString());
            if (currentPl == null)
            {
                currentPl = new Player(Context.User.Id.ToString());
                MagirecoInfoSvc.Player.UpsertItem(currentPl, currentPl.DiscordId);
                await ReplyMentionAsync($"歡迎初次使用好友系統～已登錄!");
            }
            else if (string.IsNullOrEmpty(currentPl.Description))
            {
                await ReplyMentionAsync($"尚未填寫個人簡介。");
            }
            else
            {
                await ReplyMentionAsync($"目前的個人簡介為:\n> {currentPl.Description}");
            }

            while (true)
            {
                var promptMsg = await ReplyMentionAsync($"請輸入新的個人簡介。**(字數50字以內。)**");

                var userAnsMsg = await ReponseSvc.WaitForMessageAsync((msg) =>
                {
                    return (!msg.Author.IsBot && msg.Channel.Id == Context.Channel.Id);
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
                    await ReplyMentionAsync($":warning: 字數不得超過50字!");
                    continue;
                }

                var confirmMsg = await ReplyMentionAsync($"> {userAnsMsg.Content}\n確定嗎?");
                await confirmMsg.AddReactionAsync(new Discord.Emoji("✅"));
                await confirmMsg.AddReactionAsync(new Discord.Emoji("❎"));

                var ract = await ReponseSvc.WaitForReactionAsync((cache, ch, r) =>
                {
                    return ch.Id == Context.Channel.Id &&
                    (r.Emote.Name == "✅" || r.Emote.Name == "❎");
                });

                if (ract.Emote.Name == "✅")
                {
                    currentPl.Description = userAnsMsg.Content;
                    
                    if (MagirecoInfoSvc.Player.UpsertItem(currentPl, currentPl.DiscordId))
                    {
                        await ReplyMentionAsync($"個人簡介更新成功。");
                    }
                    else
                    {
                        await ReplyMentionAsync($":warning: 個人簡介更新失敗。");
                    }
                    
                }
                else
                {
                    await ReplyAsync($"已放棄變更個人簡介。");
                    currentPl = null;
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

            
            switch (userChoice)
            {
                default:
                case int.MinValue:
                    secondEmbed = null;
                    secondContent = $"> {Context.User.Mention} 已取消。";
                    break;
                case -1:
                    secondEmbed = null;
                    secondContent = $"> {Context.User.Mention} 操作逾時。";
                    break;
                case 1:
                    await EditPlayerDescription();
                    await msgEntryPoint.DeleteAsync();
                    return;
                case 2:
                    await EditGameAccountAsync();
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
