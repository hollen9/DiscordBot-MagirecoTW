using Discord;
using Discord.Audio;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using Hollen9.NetaSoundIndex;

namespace MitamaBot.Services
{
    /// <summary>
    /// Single guild single channel
    /// </summary>
    public class NetaSoundService
    {
        //private readonly ConcurrentDictionary<ulong, AudioChannelInfo> ConnectedChannels = new ConcurrentDictionary<ulong, AudioChannelInfo>();


        private IAudioClient UsingAudioClient { get; set; }
        private IVoiceChannel UsingVoiceChannel { get; set; }
        public NetaSoundIndexEngine NetaSoundIndex { get; set; }

        public NetaSoundService()
        {
            NetaSoundIndex = new NetaSoundIndexEngine(@"C:\NetaSound");
        }

        //public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        //{
        //    AudioChannelInfo info;
        //    if (ConnectedChannels.TryGetValue(guild.Id, out info) && info.AudioClient != null)
        //    {
        //        return;   
        //    }
        //    if (target.Guild.Id != guild.Id)
        //    {
        //        return;
        //    }

        //    var audioClient = await target.ConnectAsync();
        //    if (info == null)
        //    {
        //        info = new AudioChannelInfo();
        //    }
        //    info.AudioClient = audioClient;

        //    if (ConnectedChannels.TryAdd(guild.Id, info))
        //    {
        //        // If you add a method to log happenings from this service,
        //        // you can uncomment these commented lines to make use of that.
        //        //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
        //        Console.WriteLine($"Connected to voice on {guild.Name}.");
        //    }
        //}

        //public async Task LeaveAudio(IGuild guild)
        //{
        //    AudioChannelInfo info;
        //    if (ConnectedChannels.ContainsKey(guild.Id))
        //    {
        //        info = ConnectedChannels[guild.Id];
        //        await info.AudioClient.StopAsync();
        //        ConnectedChannels.Remove(guild.Id, out info);

        //        //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");

        //    }
        //}

        public async Task JoinAudio(IVoiceChannel target)
        {
            UsingAudioClient = await target.ConnectAsync();
            UsingVoiceChannel = target;
            //Console.WriteLine($"Connected to voice on {guild.Name}.");
        }

        public async Task LeaveAudio()
        {
            await UsingAudioClient.StopAsync();
            UsingAudioClient = null;
            UsingVoiceChannel = null;
            //ConnectedChannels.Remove(guild.Id, out info);

            //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");

           
        }

        public async Task SendAudioAsync(IMessageChannel channel, string path)
        {
            // Your task: Get a full path to the file if the value of 'path' is only a filename.
            if (!File.Exists(path))
            {
                await channel.SendMessageAsync("File does not exist.");
                return;
            }
            
            if (UsingAudioClient != null)
            {
                //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
                using (var ffmpeg = CreateProcess(path))
                using (var stream = UsingAudioClient.CreatePCMStream(AudioApplication.Music))
                {
                    try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }
                }
            }
        }

        private Process CreateProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                //Arguments = $"-i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                //Arguments = $"-i \"{path}\" -ac 2 -f s16le -ar 44100 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }


        public async Task PlayByAliasAsync(IVoiceState userVoiceState, IMessageChannel userChannel, string alias)
        {
            if (UsingAudioClient == null)
            {
                await JoinAudio(userVoiceState.VoiceChannel);
            }
            else //本來就連上了
            {
                //有使用者在不同頻道請求播放
                if (UsingVoiceChannel != userVoiceState.VoiceChannel)
                {
                    await LeaveAudio();
                    await JoinAudio(userVoiceState.VoiceChannel);
                }
            }
            var queryNetaTags = NetaSoundIndex.QueryNetaItemsByAlias(alias);
            var random = new Random();
            var selectedNeta = queryNetaTags[random.Next(0, queryNetaTags.Count - 1)];

            

            StringBuilder msgToSend = new StringBuilder(string.Join(", ", selectedNeta.Characters));
            string relative_uri = Path.GetFileName(Path.GetDirectoryName(selectedNeta.Filename)) + "/" + Path.GetFileName(selectedNeta.Filename);

            if (NetaSoundIndex.Filename_CaptionItem.TryGetValue(relative_uri, out var captionItem) && captionItem != null)
            {
                msgToSend.Append(": ");
                msgToSend.Append(captionItem.Raw);
            }
            await userChannel.SendMessageAsync($"{msgToSend.ToString()}");
            await SendAudioAsync(userChannel, selectedNeta.Filename);
        }
    }
}
