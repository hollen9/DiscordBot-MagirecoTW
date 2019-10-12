using Hollen9.NetaSoundIndex.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // *To do
using Dasync.Collections;
using System.Threading;

namespace Hollen9.NetaSoundIndex
{
    public class LogEventArgs
    {
        public LogEventArgs(string msg) { Message = msg; }
        public string Message { get; } // readonly
    }

    //public class NetaSoundIndexEngineOptions
    //{
    //    public string[] AcceptFileExtensions = 
    //}

    // Declare
    public partial class NetaSoundIndexEngine
    {
        private SortedList<string, int> SortedTitles { get; }
        private Dictionary<string, IDictionary<Guid, SourceItem>> Title_SourceItems { get; }
        private Dictionary<string, IList<FileNetaTag>> Title_NetaTags { get; }
        private Dictionary<string, IList<NetaAccessIndex>> AliasKeyword_FileNetaTagIndex { get; }
        private Dictionary<string, IList<NetaAccessIndex>> CharacterName_FileNetaTagIndex { get; }
        private Dictionary<string, IList<NetaAccessIndex>> SourceTitle_FileNetaTagIndex { get; }
        public Dictionary<string, CaptionItem> Filename_CaptionItem { get; set; }

        //public Dictionary<string, int> SourceTitle_

        public delegate void LoggingHandler(object sender, LogEventArgs e);
        public event LoggingHandler Logging;
        protected virtual void OnLogging(string msg)
        {
            // Raise the event by using the () operator.
            if (Logging != null)
                Logging(this, new LogEventArgs(msg));
        }
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

            var fileToCaptionJsonPath = Path.Combine(base_path, "fileToCaption.json");
            if (File.Exists(fileToCaptionJsonPath))
            {
                try
                {
                    var jsonText = File.ReadAllText(fileToCaptionJsonPath, Encoding.UTF8);
                    Filename_CaptionItem = JsonConvert.DeserializeObject<Dictionary<string, CaptionItem>>(jsonText) ?? new Dictionary<string, CaptionItem>();
                }
                catch (JsonReaderException jsonReadEx)
                {
                    OnLogging($"{fileToCaptionJsonPath} contains BAD JSON. Maybe it's due to file encoding error? It expects UTF-8.\nJsonReaderException\n{jsonReadEx}\n\n");
                }
                catch (JsonSerializationException jsonSerialEx)
                {
                    OnLogging($"{fileToCaptionJsonPath} format is wrong. \nJsonSerializationException\n{jsonSerialEx}\n\n");
                }
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

                            OnLogging(
                                $"Source GUID: {sourceEntry.Key}\n" +
                                $"\tTitle: {sourceEntry.Value.Title}\n" +
                                $"\t Urls: {string.Join(",", sourceEntry.Value.Urls)}\n\n"
                                );
                        }
                    }
                    catch (JsonReaderException jsonReadEx)
                    {
                        OnLogging($"{seriesJsonPath} contains BAD JSON. Maybe it's due to file encoding error? It expects UTF-8.\nJsonReaderException\n{jsonReadEx}\n\n");
                    }
                    catch (JsonSerializationException jsonSerialEx)
                    {
                        OnLogging($"{seriesJsonPath} format is wrong. \nJsonSerializationException\n{jsonSerialEx}\n\n");
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

                            string segmentWithoutPrefix = segment.Remove(0, 1);
                            switch (segment[0])
                            {
                                case '@': // character's name
                                    netaTag.Characters = segmentWithoutPrefix.Split(',');
                                    break;
                                case '$': // source guid
                                    Guid.TryParse(segmentWithoutPrefix, out var guid);
                                    netaTag.SourceGuid = guid;
                                    break;
                                case '=': // alias and desciption
                                    netaTag.Alias = segmentWithoutPrefix.Split(',');
                                    break;
                                case '&': // author Discord Id
                                    var id_list = new List<long>();
                                    Array.ForEach(segmentWithoutPrefix.Split(','), x =>
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
                        segments = null;

                        //Indexing alias
                        IteratingIndex_ArrayCommonLogic(netaTag, title, netaTag.Alias, AliasKeyword_FileNetaTagIndex,
                            () => { return netaTag.Alias != null; },
                            () => OnLogging($"Warning! The NETA doesn't have alias naming tag: {netaTag.Filename}"));

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
        /// <param name="isSearch">詳細檢索 (CPU 運算會花時間)</param>
        /// <returns></returns>
        public IList<QueryNetaTag> QueryNetaItemsBySourceTitle(string sourceTitle, bool isSearch = false)
        {
            var rigid_result = QueryNetaItemsBy_MultipleTagsCommonLogic(sourceTitle, SourceTitle_FileNetaTagIndex);
            if (rigid_result == null || rigid_result.Count == 0)
            {
                if (isSearch)
                {
                    var search_result = new List<QueryNetaTag>();
                    foreach (var entry in SourceTitle_FileNetaTagIndex)
                    {
                        //出處標題 部分吻合
                        if (entry.Key.Contains(sourceTitle))
                        {
                            var possibleAccessIndices = SourceTitle_FileNetaTagIndex[entry.Key];
                            foreach (var possibleAccessIndex in possibleAccessIndices)
                            {
                                string title = SortedTitles.Keys[possibleAccessIndex.TitleIndex];
                                var netaItem = Title_NetaTags[title][possibleAccessIndex.NetaTagIndex];
                                var qNetaItem = new QueryNetaTag();
                                qNetaItem.DeepCopy(netaItem);
                                qNetaItem.Title = title;

                                if (Title_SourceItems.TryGetValue(title, out var title_srcItems))
                                {
                                    if (title_srcItems.TryGetValue(netaItem.SourceGuid, out SourceItem sourceItem))
                                    {
                                        qNetaItem.Source = sourceItem;
                                    }
                                }

                                search_result.Add(qNetaItem);
                            }
                        }
                    }
                    return search_result;
                }
            }
            return rigid_result;
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
                    if (!accessIndeices.ContainsKey(x))
                    {
                        accessIndeices.Add(x, new List<NetaAccessIndex>());
                    }

                    var indexInfo = new NetaAccessIndex()
                    {
                        NetaTagIndex = Title_NetaTags[title].IndexOf(netaTag),
                        TitleIndex = SortedTitles.ContainsKey(title) ? SortedTitles[title] : -1
                    };

                    if (indexInfo.NetaTagIndex > -1 &&
                        indexInfo.TitleIndex > -1)
                    {
                        accessIndeices[x].Add(indexInfo);
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
