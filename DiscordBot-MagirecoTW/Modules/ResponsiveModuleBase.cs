using Discord;
using Discord.Commands;
using Discord.Rest;
using MitamaBot.Helpers;
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
        /// <param name="msgBody">訊息本體，若為 null 則會發一個新的，並於 validDo 傳回 msgBody : IUserMessage。</param>
        /// <param name="title">問題標題，也就是 Embed 的標題。</param>
        /// <param name="optionsTexts">選項</param>
        /// <param name="startNumber">選項起始數字</param>
        /// <param name="isCancellable">是否可被使用者取消</param>
        /// <param name="validDo">若為有效答案做...?</param>
        /// <param name="cancelDo">若使用者取消做...?</param>
        /// <param name="timeoutDo">若逾時未答做...?</param>
        /// <param name="unknownDo">若出現例外狀況做...?</param>
        /// <returns></returns>
        public async Task<bool> AskNumberQuestion(IUserMessage msgBody, string title, string[] optionsTexts, int startNumber, bool isCancellable, Action<int, IUserMessage> validDo, Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = DiscordEmbedHelper.BuildLinesOfOptions(title, optionsTexts.ToList(), startNumber, ReponseSvc.Options.CancelKeywords);
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
                    validDo.Invoke((int)userChoseNumber, msgBody);
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
            }
            return false;
        }

        /// <summary>
        /// 詢問是非題
        /// <para>若有回答是非，則回答 True。</para>
        /// </summary>
        /// <param name="msgBody">訊息本體，若為 null 則會發一個新的，並於 validDo 傳回 msgBody : IUserMessage。</param>
        /// <param name="title">問題標題，也就是 Embed 的標題。</param>
        /// <param name="contentConfirmation">問題內文</param>
        /// <param name="isCancellable">是否可被使用者取消</param>
        /// <param name="validDo">若為有效答案做...?</param>
        /// <param name="cancelDo">若使用者取消做...?</param>
        /// <param name="timeoutDo">若逾時未答做...?</param>
        /// <param name="unknownDo">若出現例外狀況做...?</param>
        /// <returns></returns>
        public async Task<bool> AskBooleanQuestion(IUserMessage msgBody, string title, string contentConfirmation, bool isCancellable, Action<bool, IUserMessage> validDo, Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
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
                    validDo.Invoke((bool)userChoseBoolean, msgBody);
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
            }
        }
    }
}
