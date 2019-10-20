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

        public async Task<bool> AskBooleanQuestion(IUserMessage msgBody, string title, string contentConfirmation, bool isCancellable, Action<bool, IUserMessage> validDo, Action cancelDo = null, Action timeoutDo = null, Action unknownDo = null)
        {
            Embed preEmbed;
            string preContent;

            preEmbed = null;
            preContent = $"{Context.User.Mention} {contentConfirmation}";

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
