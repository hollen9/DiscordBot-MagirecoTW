using System;
using System.Threading.Tasks;
using Hollen9.NetaSoundIndex;
using Newtonsoft.Json;

namespace NetaSoundIndexEngineTestConsole
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private NetaSoundIndexEngine NetaSound;

        public async Task MainAsync()
        {
            NetaSound = new NetaSoundIndexEngine(@"C:\NetaSound");

            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var a = NetaSound.QueryNetaItemsByAlias("!");
                sw.Stop();
                if (a != null && a.Count > 0)
                {
                    Console.WriteLine($"=\"!\" 's QueryResult ({sw.Elapsed}): \n");

                    foreach (var q in a)
                    {

                        Console.Write($"{q.Filename}, ");
                    }
                }
                sw.Reset();

                sw.Start();
                var qCharacters = NetaSound.QueryNetaItemsByCharacter("七海やちよ");
                sw.Stop();
                if (qCharacters != null && qCharacters.Count > 0)
                {
                    Console.WriteLine($"\n\n@@\"七海やちよ\" 's QueryResult: \n");
                    foreach (var q in qCharacters)
                    {
                        Console.Write($"{q.Filename}, ");
                    }
                }

                var qSourceTitle = NetaSound.QueryNetaItemsBySourceTitle("魔", true);
                if (qSourceTitle != null && qSourceTitle.Count > 0)
                {
                    Console.WriteLine($"@@\"魔法少女\" 's QueryResult: \n");
                    foreach (var q in qSourceTitle)
                    {
                        Console.Write($"{q.Filename}, ");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            

            await Task.Delay(-1);
        }
    }
}
