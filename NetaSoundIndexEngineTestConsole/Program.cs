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
                var a = NetaSound.QueryNetaItemsByAlias("!");
                if (a != null && a.Count > 0)
                {
                    Console.WriteLine($"QueryResult: \n");

                    foreach (var q in a)
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
