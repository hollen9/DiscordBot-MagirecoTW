using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MitamaBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MitamaBot.Modules
{
    public class ResponsiveModuleBase : BasicModuleBase
    {
        public ResponsiveService ReponseSvc { get; set; }
        /// <summary>
        /// 詢問選擇題
        /// <para>若有回答數字，則回答 True。</para>
        /// </summary>
        /// <param name="msgBody">訊息本體，若為 null 則會發一個新的</param>
        /// <param name="title">問題標題，也就是 Embed 的標題。</param>
        /// <param name="optionsTexts">選項</param>
        /// <param name="startNumber">選項起始數字</param>
        /// <param name="isCancellable">是否可被使用者取消</param>
        /// <param name="validDo">若為有效答案做...?</param>
        /// <param name="cancelDo">若使用者取消做...?</param>
        /// <param name="timeoutDo">若逾時未答做...?</param>
        /// <param name="unknownDo">若出現例外狀況做...?</param>
        /// <returns></returns>
        public async Task<bool> AskNumberQuestion(IUserMessage msgBody, Action<IUserMessage> modifiedMsg, string title, string[] optionsTexts, int startNumber, bool isCancellable, Action<int> validDo, Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = BuildLinesOfOptions(title, optionsTexts.ToList(), startNumber, true);
            preContent = null;

            if (msgBody == null)
            {
                msgBody = await ReplyAsync(preContent, false, preEmbed);
            }
            else
            {
                await msgBody.ModifyAsync(x => {
                    x.Content = preContent;
                    x.Embed = preEmbed;
                });
            }

            try
            {
                List<Emoji> preEmojiButtons = ReponseSvc.GetNumberOptionsEmojis(optionsTexts.Length, startNumber, isCancellable);

                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msgBody.AddReactionsAsync(preEmojiButtons.ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));

                int? userChoseNumber = await ReponseSvc.WaitForNumberAnswerAsync(Context.Channel.Id, preEmojiButtons, startNumber, isCancellable);
                tcs_react_adding.Cancel();

                if (userChoseNumber == null)
                {
                    preEmbed = null;
                    preContent = $"{Context.User.Mention} 已取消。";
                    if (cancelDo != null)
                    {
                        cancelDo?.Invoke();
                    }
                    return false;
                }
                else if (userChoseNumber >= 0 + startNumber && userChoseNumber - startNumber < optionsTexts.Length)
                {
                    preEmbed = null;
                    preContent = null;
                    validDo.Invoke((int)userChoseNumber);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (timeoutDo != null)
                {
                    timeoutDo.Invoke();
                }
                return false;
            }
            catch (Exception ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (unknownDo != null)
                {
                    unknownDo.Invoke();
                }
                return false;
            }
            finally
            {
                await msgBody.RemoveAllReactionsAsync();
                if (preEmbed != null || preContent != null)
                {
                    await msgBody.ModifyAsync(x => {
                        x.Content = preContent;
                        x.Embed = preEmbed;
                    });
                }

                if (msgBody != null)
                {
                    modifiedMsg.Invoke(msgBody);
                }
            }
            return false;
        }

        /// <summary>
        /// 詢問是非題
        /// <para>若有回答是非，則回答 True。</para>
        /// </summary>
        /// <param name="msgBody">訊息本體，若為 null 則會發一個新的</param>
        /// <param name="title">問題標題，也就是 Embed 的標題。</param>
        /// <param name="contentConfirmation">問題內文</param>
        /// <param name="isCancellable">是否可被使用者取消</param>
        /// <param name="validDo">若為有效答案做...?</param>
        /// <param name="cancelDo">若使用者取消做...?</param>
        /// <param name="timeoutDo">若逾時未答做...?</param>
        /// <param name="unknownDo">若出現例外狀況做...?</param>
        /// <returns></returns>
        public async Task<bool> AskBooleanQuestion(IUserMessage msgBody, Action<IUserMessage> modifiedMsg, string title, string contentConfirmation, bool isCancellable, Action<bool> validDo, Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(contentConfirmation)
                .WithFooter(isCancellable ? $"取消指令: {string.Join("、", ReponseSvc.Options.CancelKeywords)} " : null)
                .Build();
            preContent = null;

            if (msgBody == null)
            {
                msgBody = await ReplyAsync(preContent, false, preEmbed);
            }
            else
            {
                await msgBody.ModifyAsync(x => {
                    x.Content = preContent;
                    x.Embed = preEmbed;
                });
            }

            try
            {
                List<Emoji> preEmojiButtons = ReponseSvc.GetBooleanOptionsEmojis(isCancellable);

                //Fire-and-forget (without waiting for completion)
                CancellationTokenSource tcs_react_adding = new CancellationTokenSource();
                await Task.Factory.StartNew(async () => await msgBody.AddReactionsAsync(preEmojiButtons.ToArray(), new RequestOptions() { CancelToken = tcs_react_adding.Token }));

                bool? userChoseBoolean = await ReponseSvc.WaitForBooleanAnswerAsync(Context.Channel.Id, isCancellable);
                tcs_react_adding.Cancel();

                if (userChoseBoolean == null)
                {
                    preEmbed = null;
                    preContent = $"{Context.User.Mention} 已取消。";
                    if (cancelDo != null)
                    {
                        cancelDo?.Invoke();
                    }
                    return false;
                }
                else
                {
                    preEmbed = null;
                    preContent = null;
                    validDo.Invoke((bool)userChoseBoolean);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (timeoutDo != null)
                {
                    timeoutDo.Invoke();
                }
                return false;
            }
            catch (Exception ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (unknownDo != null)
                {
                    unknownDo.Invoke();
                }
                return false;
            }
            finally
            {
                await msgBody.RemoveAllReactionsAsync();
                if (preEmbed != null || preContent != null)
                {
                    await msgBody.ModifyAsync(x => {
                        x.Content = preContent;
                        x.Embed = preEmbed;
                    });
                }
                if (msgBody != null)
                {
                    modifiedMsg.Invoke(msgBody);
                }
            }
        }

        public async Task<bool> AskTextQuestion(
            IUserMessage msgBody,
            Action<IUserMessage> msgModified,
            string title,
            string contentQuestion,
            bool isCancellableByButton,
            bool isCancellableByKeyword,
            Action<string> validDo,
            int maxRetries = 3,
            List<Func<SocketMessage, bool>> conditions = null,
            List<Action<SocketMessage, int>> conditionFailDo = null,
            Action tooManyFail = null,
            Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(contentQuestion)
                .WithFooter(isCancellableByButton ? $"取消指令: {string.Join("、", ReponseSvc.Options.CancelKeywords)} " : null)
                .Build();
            preContent = null;

            if (msgBody == null)
            {
                msgBody = await ReplyAsync(preContent, false, preEmbed);
            }
            else
            {
                await msgBody.ModifyAsync(x => {
                    x.Content = preContent;
                    x.Embed = preEmbed;
                });
            }

            await msgBody.AddReactionAsync(ReponseSvc.CancelEmoji);

            //bool isCancelled = false;
            int attempts = 0;

            ResponsiveService.SocketMessageCancellable userAnsMsg = null;

            try
            {
                do
                {
                    attempts++;

                    userAnsMsg = await ReponseSvc.WaitForMessageCancellableAsync(Context.Channel.Id, isCancellableByKeyword);
                    bool isRestart = false;

                    if (userAnsMsg.IsCancelled)
                    {
                        
                        break;
                    }

                    if (conditions != null && conditions.Count > 0)
                    {
                        for (int i = 0; i < conditions.Count; i++)
                        {
                            if (conditions[i] == null)
                            {
                                continue; // Next For-loop
                            }
                            if (i >= conditionFailDo.Count)
                            {
                                // 已經找不到對應的 Fail Do 了。
                                break;
                            }
                            if (conditionFailDo[i] == null)
                            {
                                continue; // Next For-loop
                            }
                            if (!conditions[i].Invoke(userAnsMsg.Message))
                            {
                                conditionFailDo[i].Invoke(userAnsMsg.Message, attempts);
                                isRestart = true; // Go to the end of the While-loop
                                userAnsMsg = null;
                                break; 
                            }
                        }
                    }
                    if (isRestart)
                    {
                        continue;
                    }
                    break;
                }
                while (attempts <= maxRetries);

                if (userAnsMsg == null)
                {
                    if (attempts > maxRetries)
                    {
                        //TooManyAttempts
                        if (tooManyFail != null)
                        {
                            tooManyFail.Invoke();
                        }
                        preContent = $"{Context.User.Mention} 錯誤超過 {maxRetries} 次，已取消。";
                        preEmbed = null;
                        return false;
                    }
                    else
                    {
                        throw new Exception("錯誤未達上限，但遇到錯誤所以中止。");
                    }
                }
                else if (userAnsMsg.IsCancelled)
                {
                    //Cancelled
                    if (cancelDo != null)
                    {
                        cancelDo.Invoke();
                    }
                    preContent = $"{Context.User.Mention} 已取消。";
                    preEmbed = null;
                    return false;
                }
                else
                {
                    // Got answer
                    preEmbed = null;
                    preContent = null;
                    validDo.Invoke(userAnsMsg.Message.Content);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (timeoutDo != null)
                {
                    timeoutDo.Invoke();
                }
                return false;
            }
            catch (Exception ex)
            {
                preEmbed = null;
                preContent = $"{Context.User.Mention} {ex.Message}";
                if (unknownDo != null)
                {
                    unknownDo.Invoke();
                }
                return false;
            }
            finally
            {
                await msgBody.RemoveAllReactionsAsync();
                if (preEmbed != null || preContent != null)
                {
                    await msgBody.ModifyAsync(x => {
                        x.Content = preContent;
                        x.Embed = preEmbed;
                    });
                }
                if (msgBody != null)
                {
                    msgModified.Invoke(msgBody);
                }
            }
        }


        public async Task SendTextDialog(IUserMessage msgBody, Action<IUserMessage> modifiedMsg, string title, string content)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(content)
                .Build();
            preContent = null;

            if (msgBody == null)
            {
                msgBody = await ReplyAsync(preContent, false, preEmbed);
            }
            else
            {
                await msgBody.ModifyAsync(x => {
                    x.Content = preContent;
                    x.Embed = preEmbed;
                });
            }
        }

        public Embed BuildLinesOfOptions(string title, List<string> options, int start = 1, bool isShowCancelCommandHint = true)
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
            if (isShowCancelCommandHint)
            {
                eB.WithFooter($"取消指令: {string.Join("、", ReponseSvc.Options.CancelKeywords)} ");
            }
            eB.Description = sB.ToString();
            return eB.Build();
        }
    }
}
