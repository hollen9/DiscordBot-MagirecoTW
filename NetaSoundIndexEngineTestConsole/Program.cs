using System;
using System.Threading.Tasks;
using NetaSoundIndex;
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


            await Task.Delay(-1);
        }
    }
}
