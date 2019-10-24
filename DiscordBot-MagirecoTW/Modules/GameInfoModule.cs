using Discord;
using Discord.Commands;
using MitamaBot.DataModels.Magireco;
using MitamaBot.Services;
using MitamaBot.Services.DataStore;
using MitamaBot.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MitamaBot.Modules
{
    // 相依性注入、函式
    public partial class GameInfoModule : ResponsiveModuleBase
    {
        public MagirecoInfoService MagirecoInfoSvc { get; set; }

        private EmbedBuilder GetPlayerAccountEmbedBuilder(PlayerAccount playerAccount)
        {
            var followers = MagirecoInfoSvc.FollowingInfo.FindMyFollowerAccount(playerAccount.Id).ToList();
            var followings = MagirecoInfoSvc.FollowingInfo.FindMyFollowingAccount(playerAccount.Id).ToList();

            var accountFieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder{ Name = "暱稱", Value = playerAccount.GameHandle ?? "--", IsInline = true },
                    new EmbedFieldBuilder{ Name = "等級", Value = playerAccount.GameLevel == 0 ? "--" : playerAccount.GameLevel.ToString(), IsInline = true },
                    //new EmbedFieldBuilder{ Name = "鏡層牌位", Value = "無", IsInline = true },
                    new EmbedFieldBuilder{ Name = "Following", Value = followings.Count, IsInline = true },
                    new EmbedFieldBuilder{ Name = "Follower", Value = followers.Count, IsInline = true },
                    //new EmbedFieldBuilder{ Name = "上次更新", Value = "2019/10/31", IsInline = true }
                };
            var accountEB = new EmbedBuilder
            {
                ImageUrl = playerAccount.ProfileImageUrl,
                Fields = accountFieldBuilders
            };

            return accountEB;
        }
    }

    // 測試
    public partial class GameInfoModule
    {
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

            await msgBody.ModifyAsync(x =>
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
            await msgPanel.ModifyAsync(x => x.Content = $"You chose {((bool)answer.Value ? "Yes" : "No")}");
        }
    }

    // 管理員
    public partial class GameInfoModule
    {
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
    }

    // 一般使用者
    public partial class GameInfoModule
    {        
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

            IEnumerable<string> accountShortInfos = playerAccountsOfChoseServer.Select(x => new string($"{x.GameId}"));
            List<string> preOptionsTexts = new List<string>(accountShortInfos);

            int optionStartNumber;
            if (playerAccountsOfChoseServer.Count < 2)
            {
                preOptionsTexts.Insert(0, "【新增帳號】");
                optionStartNumber = 0;
            }
            else
            {
                optionStartNumber = 1;
            }
            if (playerAccountsOfChoseServer.Count >= 1)
            {
                preOptionsTexts.Add("【刪除帳號】");
            }
            
            preEmbed = BuildLinesOfOptions(
                $"__{choseServer.ChineseName}__ 帳號編輯", preOptionsTexts, optionStartNumber, true, "【單一伺服器帳號登錄上限為2】\n").Build();
            preContent = null;

            await msgPanel.ModifyAsync(x => {
                x.Content = preContent;
                x.Embed = preEmbed;
            });

            try
            {
                preEmojiButtons = ReponseSvc.GetNumberOptionsEmojis(preOptionsTexts.Count, optionStartNumber, true);

                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msgPanel.AddReactionsAsync(preEmojiButtons.ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));

                userChoseNumber = await ReponseSvc.WaitForNumberAnswerAsync(Context.Channel.Id, preEmojiButtons, optionStartNumber, isCancellable);
                tcs_react_adding.Cancel();

                if (userChoseNumber == null)
                {
                    preEmbed = null;
                    preContent = $"{Context.User.Mention} 已取消。";
                    return;
                }
                else if (userChoseNumber >= optionStartNumber && userChoseNumber < preOptionsTexts.Count)
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
            else if (optionStartNumber == 0 && userChoseNumber == 0)
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
            // 刪除
            else if (playerAccountsOfChoseServer.Count >= 1 && (int)userChoseNumber == preOptionsTexts.Count - 1)
            {
                do
                {
                    var delWhichAns = await AskNumberQuestion(msgPanel, x => msgPanel = x,
                        "帳號刪除", accountShortInfos.ToArray(), 1, true);
                    if (!delWhichAns.IsUserAnswered || delWhichAns.IsCancelled)
                    {
                        return;
                    }
                    await msgPanel.ModifyAsync(x => x.Content = Context.User.ContentWithMention($"你選擇了 {playerAccountsOfChoseServer[(int)delWhichAns.Value - 1].GameId}"));
                    await Task.Delay(1500);
                } while (true);
            }
            else
            {
                chosePlayerAccount = playerAccountsOfChoseServer[(int)userChoseNumber - 1 + optionStartNumber];
            }

            bool flagIsFirstRunLoop = true;
            do
            {
                var followers = MagirecoInfoSvc.FollowingInfo.FindMyFollowerAccount(chosePlayerAccount.Id).ToList();
                var followings = MagirecoInfoSvc.FollowingInfo.FindMyFollowingAccount(chosePlayerAccount.Id).ToList();

                if (!flagIsFirstRunLoop)
                {
                    chosePlayerAccount = MagirecoInfoSvc.PlayerAccount.GetItem(chosePlayerAccount.Id);
                    await Task.Delay(1500);
                }
                flagIsFirstRunLoop = false;
                flagIsDialogEnded = false;
                var optionsTexts = new string[]
                {
                    "變更等級/暱稱",
                    "變更簡介",
                    "變更截圖",
                    "新增Follow",
                    "取消Follow",
                    "刪除帳號"
                };

                var accountFieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder{ Name = "暱稱", Value = chosePlayerAccount.GameHandle ?? "--", IsInline = true },
                    new EmbedFieldBuilder{ Name = "等級", Value = chosePlayerAccount.GameLevel == 0 ? "--" : chosePlayerAccount.GameLevel.ToString(), IsInline = true },
                    //new EmbedFieldBuilder{ Name = "鏡層牌位", Value = "無", IsInline = true },
                    new EmbedFieldBuilder{ Name = "Following", Value = followings.Count, IsInline = true },
                    new EmbedFieldBuilder{ Name = "Follower", Value = followers.Count, IsInline = true },
                    //new EmbedFieldBuilder{ Name = "上次更新", Value = "2019/10/31", IsInline = true }
                };

                var accountEB = new EmbedBuilder
                {
                    ImageUrl = chosePlayerAccount.ProfileImageUrl,
                    Fields = accountFieldBuilders
                };
                if (chosePlayerAccount.LastUpdateTimestamp != default)
                {
                    accountEB.Timestamp = chosePlayerAccount.LastUpdateTimestamp;
                }
                var playerIdMenuAnswer = await AskNumberQuestion(msgPanel, x => msgPanel = x,
                    $"【{chosePlayerAccount.GameId}】【{choseServer.ChineseName}】",
                    optionsTexts, 1, true, null, null, accountEB);
                if (!playerIdMenuAnswer.IsUserAnswered || playerIdMenuAnswer.IsCancelled)
                {
                    flagIsDialogEnded = true;
                    continue;
                }

                string preTitle = $"{chosePlayerAccount.GameId}: ";

                // 變更等級/暱稱
                if (playerIdMenuAnswer.Value == 1)
                {
                    int maxAttemptsLvl = 3;
                    string currentLvlText = chosePlayerAccount.GameLevel <= 0 ? "沒有等級紀錄，請填寫。" : $"目前等級為 `{chosePlayerAccount.GameLevel}`，若不變更請取消跳至下一步。";
                    var ansLv = await AskTextQuestion(msgPanel, x => msgPanel = x,
                        $"{preTitle}變更**等級**與暱稱", currentLvlText,
                        true, true, maxAttemptsLvl,
                        new List<Func<Discord.WebSocket.SocketMessage, bool>>
                        {
                            (msg) =>
                            {
                                if (!ushort.TryParse(msg.Content, out ushort lv))
                                {
                                    return false;
                                }
                                return lv > 0 && lv <= 300;
                            }
                        },
                        new List<Action<Discord.WebSocket.SocketMessage, int>>
                        {
                            async (msg, attempts) =>
                            {
                                await msgPanel.ModifyAsync(x=>
                                    x.Content=$"{Context.User.Mention} 請輸入 1~300 之間的整數。`還剩 {maxAttemptsLvl - attempts} 次機會`");
                            }
                        });
                    if (!ansLv.IsUserAnswered)
                    {
                        return;
                    }
                    if (!ansLv.IsCancelled)
                    {
                        var lvl = ushort.Parse(ansLv.Value);
                        chosePlayerAccount.GameLevel = lvl;
                        if (!MagirecoInfoSvc.PlayerAccount.UpdateItem(chosePlayerAccount, chosePlayerAccount.Id))
                        {
                            await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 等級變更失敗。");
                            continue;
                        }
                        await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 等級已變更為 **{lvl}**。");
                    }
                    // 變更暱稱
                    int maxAttemptsNickname = 3;
                    string currentNicknameText = chosePlayerAccount.GameLevel <= 0 ? "沒有暱稱資料，請填寫。" : $"目前暱稱為 `{chosePlayerAccount.GameHandle}`，若不變更請點選❎跳至下一步。";
                    var ansNickname = await AskTextQuestion(msgPanel, x => msgPanel = x,
                        $"{chosePlayerAccount.GameId}: 變更等級與**暱稱**", $"{currentNicknameText} *(字數≦8)*",
                        true, false, maxAttemptsNickname,
                        new List<Func<Discord.WebSocket.SocketMessage, bool>>
                        {
                            (msg) =>
                            {
                                return msg.Content.Length <= 8;
                            }
                        },
                        new List<Action<Discord.WebSocket.SocketMessage, int>>
                        {
                            async (msg, attempts) =>
                            {
                                await msgPanel.ModifyAsync(x=>
                                    x.Content=$"{Context.User.Mention} 不得超過8個字，請重新輸入。`還剩 {maxAttemptsNickname - attempts} 次機會`");
                            }
                        });
                    if (!ansNickname.IsUserAnswered)
                    {
                        return;
                    }
                    if (!ansNickname.IsCancelled)
                    {
                        chosePlayerAccount.GameHandle = ansNickname.Value;
                        if (!MagirecoInfoSvc.PlayerAccount.UpdateItem(chosePlayerAccount, chosePlayerAccount.Id))
                        {
                            await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 暱稱變更失敗。");
                            continue;
                        }
                        await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 暱稱已變更為 {ansNickname.Value}。");
                    }
                    continue;
                }
                // 變更簡介
                else if (playerIdMenuAnswer.Value == 2) 
                {
                    int maxAttempts = 3;
                    var ansComment = await AskTextQuestion(msgPanel, x => msgPanel = x,
                        $"{preTitle}變更簡介", "請輸入帳號簡介 (50字以內)", true, false, maxAttempts,
                        new List<Func<Discord.WebSocket.SocketMessage, bool>> 
                        {
                            msg => 
                            {
                                return msg.Content.Length <= 50;
                            }
                        },
                        new List<Action<Discord.WebSocket.SocketMessage, int>> 
                        {
                            async (msg, attempts) => 
                            {
                                await msgPanel.ModifyAsync(x=> 
                                    x.Content = Context.User.ContentWithMention("簡介不得超過50字！", MessageHelper.ResultKind.Forbidden));
                            }
                        });
                    if (ansComment.IsCancelled)
                    {
                        continue;
                    }
                    if (ansComment.IsUserAnswered)
                    {
                        chosePlayerAccount.Description = ansComment.Value;
                        string resultMsg;
                        if (MagirecoInfoSvc.PlayerAccount.UpdateItem(chosePlayerAccount, chosePlayerAccount.Id))
                        {
                            resultMsg = Context.User.ContentWithMention("已變更個人簡介。", MessageHelper.ResultKind.Succeed);
                        }
                        else
                        {
                            resultMsg = Context.User.ContentWithMention("個人簡介變更失敗！", MessageHelper.ResultKind.Failed);
                        }
                        await msgPanel.ModifyAsync(x => x.Content = resultMsg);
                        continue;
                    }
                }
                // 設置個人檔案圖片網址
                else if (playerIdMenuAnswer.Value == 3)
                {
                    int maxAttempts = 3;
                    var ansImgUrl = await AskTextQuestion(msgPanel, x => msgPanel = x,
                        $"{preTitle}設置個人檔案截圖", "請輸入個人檔案截圖的網址:",
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
                                    x.Content = Context.User.ContentWithMention($"請輸入有效的圖片網址。`還剩 {maxAttempts - attempts} 次機會`", MessageHelper.ResultKind.Forbidden)
                                    );
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
                        await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 個人檔案截圖更新失敗。");
                        continue;
                    }
                    await msgPanel.ModifyAsync(x => x.Content = $"{Context.User.Mention} 已更新個人檔案截圖。");
                    continue;
                }
                
            }
            while (!flagIsDialogEnded);
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

        [Command("profile", RunMode = RunMode.Async)]
        public async Task ViewProfile(IUser user = null, string serverKey = null)
        {
            string yohoMessage;
            string queryUserName;

            if (user == null)
            {
                user = Context.User;
                queryUserName = user.ToString();
                yohoMessage = $"你的個人檔案如下：";
            }
            else
            {
                if (user is IGuildUser guildUser)
                {
                    queryUserName = guildUser.Nickname ?? user.ToString();
                }
                else
                {
                    queryUserName = user.ToString();
                }
                yohoMessage = $"喲喝～ {queryUserName} 的個人檔案如下：";
            }

            var player = MagirecoInfoSvc.Player.GetItem(user.Id.ToString());
            if (player == null)
            {
                await ReplyAsync(Context.User.ContentWithMention($"查無個人檔案，可以請 {queryUserName} 建立一個。"));
                return;
            }

            await ReplyAsync(MessageHelper.ContentWithMention(Context.User, yohoMessage),false,
                new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("總個人檔案")
                .WithDescription(player.Description ?? string.Empty)
                .Build()
                );

            List<PlayerAccount> playerAccounts;
            if (serverKey == null)
            {
                playerAccounts = MagirecoInfoSvc.PlayerAccount.FindItems(x =>
                    x.OwnerDiscordId == user.Id.ToString()
                    )
                    .ToList();
            }
            else
            {
                playerAccounts = MagirecoInfoSvc.PlayerAccount.FindItems(x =>
                    x.OwnerDiscordId == user.Id.ToString() &&
                    x.OwnerServerKey == serverKey
                    )
                    .ToList();
                if (playerAccounts == null || playerAccounts.Count == 0)
                {
                    await ReplyAsync(Context.User.ContentWithMention("查無任何玩家資料。"));
                    return;
                }

                //var dict = MagirecoInfoSvc.PlayerAccount.FindAccountsByIdAndGroupByServer(user.Id);
                //if (!dict.TryGetValue(serverKey, out playerAccounts))
                //{
                //    await ReplyAsync(Context.User.ContentWithMention("查無此伺服器。"));
                //    return;
                //}
            }
            foreach (var playerAccount in playerAccounts)
            {
                var eb = this.GetPlayerAccountEmbedBuilder(playerAccount);
                await ReplyEmbedAsync(eb);
            }
        }
    }
}
