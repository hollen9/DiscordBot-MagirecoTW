using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MitamaBot.Services;

namespace MitamaBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class NetaSoundModule : MyModuleBase
    {
        // Dependency Injection will fill this value in for us
        public NetaSoundService NetaSoundSvc { get; set; }

        //// The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        //[Command("join", RunMode = RunMode.Async)]
        //public async Task JoinChannel(IVoiceChannel channel = null)
        //{
        //    await NetaSoundService.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        //}

        //// The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        //[Command("leave", RunMode = RunMode.Async)]
        //public async Task LeaveChannel()
        //{
        //    await NetaSoundService.LeaveAudio(Context.Guild);
        //}

        //[Command("playtest", RunMode = RunMode.Async)]
        //public async Task PlayTest()
        //{
        //    await NetaSoundService.SendAudioAsync(Context.Guild, Context.Channel, @"F:\Liberary\GameFiles\SteamLib\steamapps\common\Counter-Strike Global Offensive\csgo\sound\planetia\mw3\pmc_win_1.mp3");
        //}

        [Command("s", RunMode = RunMode.Async)]
        public async Task PlayNetaWithAliasAsync(string alias, IVoiceChannel channel = null)
        {
            await NetaSoundSvc.PlayByAliasAsync((Context.User as IVoiceState), Context.Channel, alias);
        }
    }
}
