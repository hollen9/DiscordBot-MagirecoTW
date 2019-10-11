using NetaSoundIndex.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NetaSoundIndex
{
    public class NetaSoundIndexEngine
    {

        public Dictionary<string, Dictionary<Guid, SourceItem>> TitleSourceInfo { get; }

        public NetaSoundIndexEngine(string base_path)
        {
            TitleSourceInfo = new Dictionary<string, Dictionary<Guid, SourceItem>>();

            if (!Directory.Exists(base_path))
            {
                throw new DirectoryNotFoundException($"{base_path} doesn't exist!");
            }
            var subDirs = Directory.GetDirectories(base_path);

            foreach (var subDir in subDirs)
            {
                string title = Path.GetFileName(subDir);
                var seriesJsonPath = Path.Combine(subDir, "sourceInfo.json");
                if (File.Exists(seriesJsonPath))
                {
                    try
                    {
                        var jsonText = File.ReadAllText(seriesJsonPath, Encoding.UTF8);
                        if (!TitleSourceInfo.ContainsKey(title))
                        {
                            TitleSourceInfo.Add(title, new Dictionary<Guid, SourceItem>());
                        }
                        var sourceInfoEntries = JsonConvert.DeserializeObject<Dictionary<Guid, SourceItem>> (jsonText);
                        
                        foreach (var sourceEntry in sourceInfoEntries)
                        {
                            TitleSourceInfo[title].Add(sourceEntry.Key, sourceEntry.Value);

                            Console.WriteLine(
                                $"Source GUID: {sourceEntry.Key}\n" +
                                $"\tTitle: {sourceEntry.Value.Title}\n" +
                                $"\t Urls: {string.Join(",", sourceEntry.Value.Urls)}\n\n"
                                );
                        }
                    }
                    catch (JsonReaderException jsonReadEx)
                    {
                        Console.WriteLine($"{seriesJsonPath} contains BAD JSON. Maybe it's due to file encoding error? It expects UTF-8.\nJsonReaderException\n{jsonReadEx}\n\n");
                    }
                    catch (JsonSerializationException jsonSerialEx)
                    {
                        Console.WriteLine($"{seriesJsonPath} format is wrong. \nJsonSerializationException\n{jsonSerialEx}\n\n");
                    }
                    
                }

                var files = Directory.GetFiles(subDir);
                foreach(var file in files)
                {
                    if (Path.GetExtension(file) == ".mp3")
                    {
                        Console.WriteLine(file + "\n");

                    }
                }
            }
        }
    }
}
