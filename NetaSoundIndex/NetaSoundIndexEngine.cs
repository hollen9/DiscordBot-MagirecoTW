using Hollen9.NetaSoundIndex.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks; // *To do

namespace Hollen9.NetaSoundIndex
{
    // Declare
    public partial class NetaSoundIndexEngine
    {
        public SortedList<string, int> SortedTitles { get; }
        public Dictionary<string, IDictionary<Guid, SourceItem>> Title_SourceItems { get; }
        public Dictionary<string, IList<FileNetaTag>> Title_NetaTags { get; }
        public Dictionary<string, IList<NetaAccessIndex>> AliasKeyword_FileNetaTagIndex { get; }
        public Dictionary<string, IList<NetaAccessIndex>> CharacterName_FileNetaTagIndex { get; }
        public Dictionary<string, IList<NetaAccessIndex>> SourceTitle_FileNetaTagIndex { get; }
    }

    // Public Methods
    public partial class NetaSoundIndexEngine
    {
        public NetaSoundIndexEngine(string base_path)
        {
            SortedTitles = new SortedList<string, int>();

            Title_SourceItems = new Dictionary<string, IDictionary<Guid, SourceItem>>();
            Title_NetaTags = new Dictionary<string, IList<FileNetaTag>>();

            AliasKeyword_FileNetaTagIndex = new Dictionary<string, IList<NetaAccessIndex>>();
            CharacterName_FileNetaTagIndex = new Dictionary<string, IList<NetaAccessIndex>>();
            SourceTitle_FileNetaTagIndex = new Dictionary<string, IList<NetaAccessIndex>>();

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
                        if (!Title_SourceItems.ContainsKey(title))
                        {
                            Title_SourceItems.Add(title, new Dictionary<Guid, SourceItem>());

                            if (!SortedTitles.ContainsKey(title))
                            {
                                SortedTitles.Add(title, SortedTitles.Count); // appear 1st times out of 2
                            }
                        }
                        var sourceInfoEntries = JsonConvert.DeserializeObject<Dictionary<Guid, SourceItem>>(jsonText);

                        foreach (var sourceEntry in sourceInfoEntries)
                        {
                            Title_SourceItems[title].Add(sourceEntry.Key, sourceEntry.Value);

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

                if (!Title_NetaTags.ContainsKey(title))
                {
                    Title_NetaTags.Add(title, new List<FileNetaTag>());

                    if (!SortedTitles.ContainsKey(title))
                    {
                        SortedTitles.Add(title, SortedTitles.Count); // appear 2nd times out of 2
                    }
                }

                // NetaSound's Filename Example: "C:\NetaSound\magirepo\@七海やちよ;$e4d592eb4a1f402f8024ee6838b50eea;=10bai,mouikanaito;&65817312732655616.mp3"
                // Filename's max characters <= 255
                var files = Directory.GetFiles(subDir);
                foreach (var filename in files)
                {

                    var filename_noext = Path.GetFileNameWithoutExtension(filename);

                    // determine if it is sound file
                    if (Path.GetExtension(filename) == ".mp3")
                    {
                        /*Console.WriteLine(filename_noext + "\n");*/
                        var segments = filename_noext.Split(';');
                        var netaTag = new FileNetaTag();

                        foreach (var segment in segments)
                        {
                            netaTag.Filename = filename;

                            if (segment.Length <= 0)
                            {
                                continue;
                            }


                            switch (segment[0])
                            {
                                case '@': // character's name
                                    netaTag.Characters = segment.Remove(0, 1).Split(',');
                                    break;
                                case '$': // source guid
                                    Guid.TryParse(segment.Remove(0, 1), out var guid);
                                    netaTag.SourceGuid = guid;
                                    break;
                                case '=': // alias and desciption
                                    netaTag.Alias = segment.Remove(0, 1).Split(',');
                                    break;
                                case '&': // author Discord Id
                                    var id_list = new List<long>();
                                    Array.ForEach(segment.Remove(0, 1).Split(','), x =>
                                    {
                                        if (long.TryParse(x, out var id))
                                        {
                                            id_list.Add(id);
                                        }
                                    });
                                    netaTag.AuthorsDiscordId = id_list.ToArray();
                                    id_list = null;
                                    break;
                            }

                            Title_NetaTags[title].Add(netaTag);
                        }

                        //Indexing alias
                        IteratingIndex_ArrayCommonLogic(netaTag, title, netaTag.Alias, AliasKeyword_FileNetaTagIndex,
                            () => { return netaTag.Alias != null; },
                            () => Console.WriteLine($"Warning! The NETA doesn't have alias naming tag: {netaTag.Filename}"));

                        //Indexing character name
                        IteratingIndex_ArrayCommonLogic(netaTag, title, netaTag.Characters, CharacterName_FileNetaTagIndex,
                            () => { return netaTag.Characters?.Length > 0; });

                        //Indexing source title
                        if (Title_SourceItems.TryGetValue(title, out var title_srcItems) &&
                            title_srcItems.TryGetValue(netaTag.SourceGuid, out SourceItem sourceItem) &&
                            !string.IsNullOrWhiteSpace(sourceItem.Title))
                        {
                            IteratingIndex_ArrayCommonLogic(netaTag, title, new string[] { sourceItem.Title }, SourceTitle_FileNetaTagIndex);
                        }
                    }
                }
            }
            // The above code already finished filling: TitleSourceItems, TitleNetaTags   
        }

        /// <summary>
        /// 按來源名稱檢索 (原文)
        /// </summary>
        /// <param name="sourceTitle"></param>
        /// <returns></returns>
        public IList<QueryNetaTag> QueryNetaItemsBySourceTitle(string sourceTitle)
        {
            return QueryNetaItemsBy_MultipleTagsCommonLogic(sourceTitle, SourceTitle_FileNetaTagIndex);
        }

        /// <summary>
        /// 按同位片語搜索 (全羅馬拼音)
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public IList<QueryNetaTag> QueryNetaItemsByAlias(string alias)
        {
            return QueryNetaItemsBy_MultipleTagsCommonLogic(alias, AliasKeyword_FileNetaTagIndex);
        }

        /// <summary>
        /// 按角色名搜索 (原文)
        /// </summary>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public IList<QueryNetaTag> QueryNetaItemsByCharacter(string characterName)
        {
            return QueryNetaItemsBy_MultipleTagsCommonLogic(characterName, CharacterName_FileNetaTagIndex);
        }
    }

    // Private Functions
    public partial class NetaSoundIndexEngine
    {
        private IList<QueryNetaTag> QueryNetaItemsBy_MultipleTagsCommonLogic(string queryText, IDictionary<string, IList<NetaAccessIndex>> accessIndeices)
        {
            if (accessIndeices.TryGetValue(queryText, out var possibleNetaItems))
            {
                var queryNetaTags = new List<QueryNetaTag>();

                foreach (var possibleNetaItem in possibleNetaItems)
                {
                    var queryNetaTag = new QueryNetaTag();

                    string title = SortedTitles.Keys[possibleNetaItem.TitleIndex];
                    queryNetaTag.Title = title;

                    var netaItem = Title_NetaTags[title][possibleNetaItem.NetaTagIndex];

                    queryNetaTag.DeepCopy(netaItem);

                    if (Title_SourceItems.TryGetValue(title, out var title_srcItems))
                    {
                        if (title_srcItems.TryGetValue(netaItem.SourceGuid, out SourceItem sourceItem))
                        {
                            queryNetaTag.Source = sourceItem;
                        }
                    }
                    queryNetaTags.Add(queryNetaTag);
                }

                return queryNetaTags;
            }
            else
            {
                return null;
            }
        }

        private void IteratingIndex_ArrayCommonLogic(FileNetaTag netaTag, string title, string[] array,
            IDictionary<string, IList<NetaAccessIndex>> accessIndeices, Func<bool> condition = null,
            Action actionIfConditionNotMeet = null)
        {
            bool conditionResult = condition != null ? condition.Invoke() : true;
            if (conditionResult)
            {
                Array.ForEach(array, x =>
                {
                    if (!CharacterName_FileNetaTagIndex.ContainsKey(x))
                    {
                        CharacterName_FileNetaTagIndex.Add(x, new List<NetaAccessIndex>());
                    }

                    var indexInfo = new NetaAccessIndex()
                    {
                        NetaTagIndex = Title_NetaTags[title].IndexOf(netaTag),
                        TitleIndex = SortedTitles.ContainsKey(title) ? SortedTitles[title] : -1
                    };

                    if (indexInfo.NetaTagIndex > -1 &&
                        indexInfo.TitleIndex > -1)
                    {
                        CharacterName_FileNetaTagIndex[x].Add(indexInfo);
                    }
                });
            }
            else if (actionIfConditionNotMeet != null)
            {
                actionIfConditionNotMeet.Invoke();
            }
        }
    }
}
