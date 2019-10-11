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

            var a = NetaSound.QueryNetaItemsByAlias("shoukai");
            if (a != null && a.Count > 0)
            {
                Console.WriteLine($"QueryResult: {a[0].Filename}");
            }
            

            await Task.Delay(-1);
        }
    }
}
