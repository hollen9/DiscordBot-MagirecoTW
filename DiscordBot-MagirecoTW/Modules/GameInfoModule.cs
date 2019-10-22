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
    public class GameInfoModule : ResponsiveModuleBase
    {
        public MagirecoInfoService MagirecoInfoSvc { get; set; }

        //[Command("test-question", RunMode = RunMode.Async)]
        //public async Task TestTextQuestionAsync()
        //{
        //    IUserMessage msgBody = null;
        //    string userAns = null;
        //    if (!await AskTextQuestion(null, msg => msgBody = msg, "Please enter some text", "text:", true, true, ans => userAns = ans))
        //    {
        //        return;
        //    }
        //    await msgBody.ModifyAsync(x=> 
        //    {
        //        x.Content = $"{Context.User.Mention} said,\n> {userAns ?? "nothing (NULL)"}";
        //        x.Embed = null;
        //    });
        //}

        [Command("test-opt", RunMode = RunMode.Async)]
        public async Task TestOptionsAsync()
        {
            //int userChoseNumber = default;
            //bool userChoseBoolean = default;
            IUserMessage msgBody = null;

            var userNumberAnswer = await AskNumberQuestion(
                null,
                msg => msgBody = msg,
                "Please choose:",
                new string[] { "Apple", "Pen" },
                1, true);
            if (!userNumberAnswer.IsUserAnswered || userNumberAnswer.IsCancelled)
            {
                return;
            }

            var userBoolAnswer = await AskBooleanQuestion(
                msgBody, msg => msgBody = msg
                ,
                "FA?",
                $"You just chose {userNumberAnswer.Value}. FA?",
                true);

            if (!userBoolAnswer.IsUserAnswered || userBoolAnswer.IsCancelled)
            {
                return;
            }

            await msgBody.ModifyAsync(x=> 
            {
                x.Content = $"User chose {userNumberAnswer.Value} and said {userBoolAnswer.Value}";
                x.Embed = null;
            });

        }

        [Command("test-bool", RunMode = RunMode.Async)]
        public async Task TestBooleanAsync()
        {
            bool isCancellable = true;
            
            var msg = await ReplyAsync("請選擇");
            try
            {
                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msg.AddReactionsAsync(ReponseSvc.GetBooleanOptionsEmojis().ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));
                bool? userChoice = await ReponseSvc.WaitForBooleanAnswerAsync(Context.Channel.Id, isCancellable, TimeSpan.FromSeconds(5));
                tcs_react_adding.Cancel();

                await msg.ModifyAsync(x =>
                {
                    if (userChoice == null)
                    {
                        x.Content = $"{Context.User.Mention} 已取消。";
                    }
                    else
                    {
                        x.Content = $"{Context.User.Mention} 選擇: **{ ((bool)userChoice ? "是" : "否") }**。";
                    }
                });
            }
            catch (TimeoutException ex)
            {
                await msg.ModifyAsync(x =>
                {
                    x.Content = $"{Context.User.Mention} {ex.Message}";
                });
            }
            catch (Exception ex)
            {
                await msg.ModifyAsync(x =>
                {
                    x.Content = $"{Context.User.Mention} {ex.Message}";
                });
            }
            finally
            {
                await msg.RemoveAllReactionsAsync();
            }
        }

        [Command("test-bool2", RunMode = RunMode.Async)]
        public async Task TestBoolean2Async()
        {
            bool isCancellable = true;
            IUserMessage msgPanel = null;
            var answer = await AskBooleanQuestion(null, x => msgPanel = x, "Yes or No?", "Please choose.", true);
            if (!answer.IsUserAnswered || answer.IsCancelled)
            {
                return;
            }
            await msgPanel.ModifyAsync(x=>x.Content = $"You chose {((bool)answer.Value ? "Yes" : "No")}");
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
            //Shortcuts / Declarances
            string userId = Context.User.Id.ToString();

            IUserMessage msgPanel;
            List<Emoji> preEmojiButtons;
            StringBuilder preContentBuilder;
            Embed preEmbed = null;
            int? userChoseNumber = null;
            bool isCancellable;

            PlayerAccount chosePlayerAccount = null;
            Server choseServer = null;
            bool flagIsDialogEnded = false;
            
            //Start
            var serversEm = MagirecoInfoSvc.Server.GetItems();

            var servers = serversEm.ToList();

            if (servers == null || servers.Count == 0)
            {
                await ReplyMentionAsync("噢噢! 好像還沒有設定任何伺服器\n先等管理員新增完成了再來使用吧");
                return;
            }

            var player = MagirecoInfoSvc.Player.GetItem(Context.User.Id.ToString());

            preContentBuilder = new StringBuilder();
            if (player == null)
            {
                var noPlWarnMsg = await ReplyAsync("由於你還沒有建置玩家檔案，所以先引導建立好，再回頭綁定ID吧~");
                await EditPlayerDescription();
                player = MagirecoInfoSvc.Player.GetItem(Context.User.Id.ToString());

                if (player == null)
                {
                    await noPlWarnMsg.ModifyAsync(msg=>
                    {
                        msg.Content = $"{Context.User.Mention} :warning: 資料存取錯誤: 個人檔案建立失敗，中止帳號綁定程序。";
                    });
                    return;
                }
            }

            var playerAccounts = MagirecoInfoSvc.PlayerAccount.FindItems(pAccount => pAccount.OwnerDiscordId == userId).ToList();

            if (playerAccounts == null || playerAccounts.Count == 0)
            {
                preContentBuilder.Append($"你還沒有綁定任何遊戲帳號，");
            }
            preContentBuilder.Append($"請問你想要查看哪個伺服器？");

            var embedPromptServer = BuildLinesOfOptions(
                "遊戲帳號伺服器", servers.Select(x => new string($"{x.ChineseName} `{x.ServerKey}`")
                ).ToList(), 1,true
                );
            msgPanel = await ReplyAsync(preContentBuilder.ToString(), false, embedPromptServer.Build());

            isCancellable = true;
            preEmojiButtons = ReponseSvc.GetNumberOptionsEmojis(servers.Count, 1, isCancellable);

            string preContent = null;

            //詢問要看哪個伺服器的帳號呢
            try
            {
                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msgPanel.AddReactionsAsync(preEmojiButtons.ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));

                userChoseNumber = await ReponseSvc.WaitForNumberAnswerAsync(Context.Channel.Id, preEmojiButtons, 1, isCancellable);
                tcs_react_adding.Cancel();

                //完成詢問目標伺服器
                
                if (userChoseNumber == null)
                {
                    preEmbed = null;
                    preContent = $"{Context.User.Mention} 已取消。";
                    return;
                }
                else if (userChoseNumber - 1 >= 0 && userChoseNumber <= servers.Count)
                {
                    choseServer = servers[(int)userChoseNumber - 1];

                    preEmbed = null;
                    preContent = null;
                }
            }
            catch (TimeoutException ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                return;
            }
            catch (Exception ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                return;
            }
            finally
            {
                await msgPanel.RemoveAllReactionsAsync();
                if (preEmbed != null || preContent != null)
                {
                    await msgPanel.ModifyAsync(x => {
                        x.Content = preContent;
                        x.Embed = preEmbed;
                    });
                }
            }
            
            if (choseServer == null)
            {
                return;
            }

            var playerAccountsOfChoseServer = MagirecoInfoSvc.PlayerAccount.FindItems(pa =>
                        pa.OwnerServerKey == choseServer.ServerKey &&
                        pa.OwnerDiscordId == userId).ToList();

            List<string> preOptionsTexts = playerAccountsOfChoseServer.Select(x => new string($"{x.GameId}")).ToList();
            preOptionsTexts.Insert(0, "【新增帳號】");

            preEmbed = BuildLinesOfOptions(
                $"__{choseServer.ChineseName}__ 帳號編輯", preOptionsTexts, 0, true).Build();
            preContent = null;

            await msgPanel.ModifyAsync(x => {
                x.Content = preContent;
                x.Embed = preEmbed;
            });

            try
            {
                preEmojiButtons = ReponseSvc.GetNumberOptionsEmojis(preOptionsTexts.Count, 0, true);

                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msgPanel.AddReactionsAsync(preEmojiButtons.ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));

                userChoseNumber = await ReponseSvc.WaitForNumberAnswerAsync(Context.Channel.Id, preEmojiButtons, 0, isCancellable);
                tcs_react_adding.Cancel();

                if (userChoseNumber == null)
                {
                    preEmbed = null;
                    preContent = $"{Context.User.Mention} 已取消。";
                    return;
                }
                else if (userChoseNumber >= 0 && userChoseNumber < preOptionsTexts.Count)
                {
                    preEmbed = null;
                    preContent = null;
                }
            }
            catch (TimeoutException ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                return;
            }
            catch (Exception ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                return;
            }
            finally
            {
                await msgPanel.RemoveAllReactionsAsync();
                if (preEmbed != null || preContent != null)
                {
                    await msgPanel.ModifyAsync(x => {
                        x.Content = preContent;
                        x.Embed = preEmbed;
                    });
                }
            }

            if (userChoseNumber == null)
            {
                return;
            }
            else if (userChoseNumber == 0)
            {
                bool isOk = false,                     
                     isAbort = false;
                int maxAttempts = 3;

                while (!isOk || isAbort)
                {
                    isAbort = false;
                    var preIdConditions = new List<Func<Discord.WebSocket.SocketMessage, bool>> 
                    {
                        userMsg => 
                        {
                            if (userMsg.Content.Length != 8)
                            {
                                return false;
                            }
                            return true;
                        }
                    };

                    var conditionsFailDo = new List<Action<Discord.WebSocket.SocketMessage, int>> {
                        async (userMsg, attempts) =>
                        {
                            if (attempts <= maxAttempts)
                            {
                                await msgPanel.ModifyAsync(x=>x.Content = $"{Context.User.Mention} 玩家ID為8碼! `(還剩{maxAttempts-attempts}次機會)`");
                            }
                        }
                    };

                    var playerIdAnswer = await AskTextQuestion(
                        msgPanel, msg => msgPanel = msg, "新增帳號", "請輸入8碼的玩家ID。", true, true, maxAttempts, preIdConditions, conditionsFailDo);

                    // 新增帳號
                    if (!playerIdAnswer.IsUserAnswered || playerIdAnswer.IsCancelled)
                    {
                        return;
                    }
                    
                    if (isAbort)
                    {
                        return;
                    }

                    var addConfirmAnswer = await AskBooleanQuestion(msgPanel, msg => msgPanel = msg,
                        "帳號新增確認", $"你確認要新增以下帳號至`{choseServer.ChineseName}`嗎?\n> **{playerIdAnswer.Value}**",
                        true);

                    if (addConfirmAnswer.Value == false)
                    {
                        continue;
                    }

                    if (addConfirmAnswer.IsCancelled || addConfirmAnswer.IsTimedout || addConfirmAnswer.IsUnknownErrorOccurred)
                    {
                        return;
                    }

                    var newAccountItem = new PlayerAccount 
                    {
                        OwnerDiscordId = Context.User.Id.ToString(),
                        OwnerServerKey = choseServer.ServerKey,
                        GameId = playerIdAnswer.Value
                    };
                    newAccountItem.Id = MagirecoInfoSvc.PlayerAccount.AddItem(newAccountItem);

                    if (newAccountItem.Id == null)
                    {
                        await msgPanel.ModifyAsync(x =>
                        {
                            x.Embed = null;
                            x.Content = $"{Context.User.Mention} ID 新增失敗。";
                        });
                        return;
                    }
                    else
                    {
                        chosePlayerAccount = newAccountItem;
                        await msgPanel.ModifyAsync(x =>
                        {
                            x.Embed = null;
                            x.Content = $"{Context.User.Mention} ID 新增成功。";
                        });
                    }
                    break;
                }
            }
            else
            {
                chosePlayerAccount = playerAccountsOfChoseServer[(int)userChoseNumber - 1];
            }

            //await Task.Delay(3000).ContinueWith(async x=> 
            //{
            //    await msgPanel.DeleteAsync();
            //});

            do
            {
                flagIsDialogEnded = false;
                var optionsTexts = new string[]
                {
                    "變更帳號暱稱",
                    "變更帳號簡介",
                    "變更帳號截圖",
                    "新增Follow",
                    "取消Follow",
                    "刪除帳號"
                };

                var accountFieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder{ Name = "等級", Value = "無", IsInline = true },
                    //new EmbedFieldBuilder{ Name = "鏡層牌位", Value = "無", IsInline = true },
                    new EmbedFieldBuilder{ Name = "Following", Value = "0", IsInline = true },
                    new EmbedFieldBuilder{ Name = "Follower", Value = "0", IsInline = true },
                    new EmbedFieldBuilder{ Name = "上次更新", Value = "2019/10/31", IsInline = true }
                };

                var playerIdMenuAnswer = await AskNumberQuestion(msgPanel, x => msgPanel = x,
                    $"【{chosePlayerAccount.GameId}】【{choseServer.ChineseName}】",
                    optionsTexts, 1, true, null, null, accountFieldBuilders, chosePlayerAccount.ProfileImageUrl);
                if (!playerIdMenuAnswer.IsUserAnswered || playerIdMenuAnswer.IsCancelled)
                {
                    flagIsDialogEnded = true;
                    continue;
                }

                if (playerIdMenuAnswer.Value == 3)
                {
                    int maxAttempts = 3;
                    var ansImgUrl = await AskTextQuestion(msgPanel, x => msgPanel = x,
                        $"{chosePlayerAccount.GameId}: 設置個人檔案截圖", "請輸入個人檔案截圖的網址:",
                        true, true, maxAttempts,
                        new List<Func<Discord.WebSocket.SocketMessage, bool>> 
                        {
                            (msg) => 
                            {
                                Uri uriResult;
                                bool result = Uri.TryCreate(msg.Content, UriKind.Absolute, out uriResult)
                                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                                return result;
                            }
                        },
                        new List<Action<Discord.WebSocket.SocketMessage, int>> 
                        {
                            async (msg, attempts) =>
                            {
                                await msgPanel.ModifyAsync(x=>
                                    x.Content=$"{Context.User.Mention} 請輸入有效的圖片網址。`還剩 {maxAttempts - attempts} 次機會`");
                            }
                        });
                    if (!ansImgUrl.IsUserAnswered)
                    {
                        return;
                    }
                    if (ansImgUrl.IsCancelled)
                    {
                        continue;
                    }

                    chosePlayerAccount.ProfileImageUrl = ansImgUrl.Value;
                    if (!MagirecoInfoSvc.PlayerAccount.UpdateItem(chosePlayerAccount, chosePlayerAccount.Id))
                    {
                        await msgPanel.ModifyAsync(x =>
                                    x.Content = $"{Context.User.Mention} 個人檔案截圖更新失敗。");
                        continue;
                    }
                    await msgPanel.ModifyAsync(x =>
                                    x.Content = $"{Context.User.Mention} 已更新個人檔案截圖。");
                    continue;
                }

                await msgPanel.ModifyAsync(x =>
                {
                    x.Content = $"選擇 {playerIdMenuAnswer.Value}";
                    x.Embed = null;
                });
            }
            while (!flagIsDialogEnded);

            //else
            //{
            //    preContent = $"選擇 {userChoseNumber}";
            //    preEmbed = null;
            //}

            //await msgPanel.ModifyAsync(x => {
            //    x.Content = preContent;
            //    x.Embed = preEmbed;
            //});





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
                
                try
                {
                    //Fire-and-forget (without waiting for completion)
                    CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                    await Task.Factory.StartNew(async () => await confirmMsg.AddReactionsAsync(ReponseSvc.GetBooleanOptionsEmojis(false).ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));
                    bool? userChoice = await ReponseSvc.WaitForBooleanAnswerAsync(Context.Channel.Id, false);
                    tcs_react_adding.Cancel();

                    await confirmMsg.ModifyAsync(x =>
                    {
                        if (userChoice == null)
                        {
                            x.Content = $"{Context.User.Mention} 已取消。";
                        }
                        else
                        {
                            x.Content = $"{Context.User.Mention} 選擇: **{ ((bool)userChoice ? "是" : "否") }**。";
                            
                            if (userChoice == true)
                            {
                                currentPl.Description = userAnsMsg.Content;

                                if (MagirecoInfoSvc.Player.UpsertItem(currentPl, currentPl.DiscordId))
                                {
                                    x.Content = $"{Context.User.Mention} 個人簡介更新成功。";
                                }
                                else
                                {
                                    x.Content = $"{Context.User.Mention} 個人簡介更新失敗。";
                                }
                            }
                            else
                            {
                                x.Content = $"{Context.User.Mention} 已取消。";
                            }
                        }
                    });
                }
                catch (TimeoutException ex)
                {
                    await confirmMsg.ModifyAsync(x =>
                    {
                        x.Content = $"{Context.User.Mention} {ex.Message}";
                    });
                }
                catch (Exception ex)
                {
                    await confirmMsg.ModifyAsync(x =>
                    {
                        x.Content = $"{Context.User.Mention} {ex.Message}";
                    });
                }
                finally
                {
                    await confirmMsg.RemoveAllReactionsAsync();
                }
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
