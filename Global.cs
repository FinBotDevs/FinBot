﻿using Discord;
using Discord.WebSocket;
using FinBot.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FinBot
{
    public class Global
    {
        public static string Token { get; set; }
        public static string Prefix { get; set; }
        public static string Version { get; set; }
        public static string YouTubeAPIKey { get; set; }
        public static uint MaxUserPingCount { get; set; }
        public static uint MaxRolePingCount { get; set; }
        public static uint LevelMultiplier { get; set; }
        public static uint MinMessageTimestamp { get; set; }
        public static string GoogleSearchAPIKey { get; set; }
        public static string GeniusAPIKey { get; set; }
        public static string LoggingLevel { get; set; }
        public static string Pythoninterpreter { get; set; }
        public static ulong SupportChannelId { get; set; }
        public static ulong SupportGuildId { get; set; }

        public class MySQL
        {
            public static string MySQLServer { get; set; }
            public static string MySQLUser { get; set; }
            public static string MySQLDatabase { get; set; }
            public static string MySQLPort { get; set; }
            public static string MySQLPassword { get; set; }
            public static string connStr { get; set; }
        }

        private static string ConfigPath = $"{Environment.CurrentDirectory}/Data/Config.json";
        public static DiscordShardedClient Client { get; set; }
        public static string WelcomeMessageURL { get; set; }
        internal static JsonItems CurrentJsonData;
        public static string KickMessageURL { get; set; }
        public static string BanMessageURL { get; set; }
        public static string infractionMessagefilepath = $"{Environment.CurrentDirectory}/Data/infractionCards.txt";
        public static string CensoredWordsPath = $"{Environment.CurrentDirectory}/Data/Censored.txt";
        public static string LeetRulesPath = $"{Environment.CurrentDirectory}/Data/LeetRules.txt";
        public static bool AutoSlowmodeEnabled { get; set; }
        public static string TopicsPath = $"{Environment.CurrentDirectory}/Data/Topics.txt";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static List<IEmote> reactions = new List<IEmote>()
                        {
                            new Emoji("✅"),
                            new Emoji("❌")
                        };
        public static List<string> hiddenCommands = new List<string> { "restart", "terminate", "updateSupport", "tld", "exec", "ayst", "Speaking", "setAudioClient" };
        public static List<ulong> DevUIDs = new List<ulong> { 305797476290527235, 368095722442194945, 230778630597246983 };


        public static void ReadConfig()
        {
            JsonItems data = JsonConvert.DeserializeObject<JsonItems>(File.ReadAllText(ConfigPath));
            CurrentJsonData = data;
            Token = data.Token;
            Prefix = data.Prefix;
            Version = data.Version;
            YouTubeAPIKey = data.YouTubeAPIKey;
            MaxUserPingCount = data.MaxUserPingCount;
            MaxRolePingCount = data.MaxRolePingCount;
            LevelMultiplier = data.LevelMultiplier;
            MinMessageTimestamp = data.MinMessageTimestamp;
            GoogleSearchAPIKey = data.GoogleSearchAPIKey;
            MySQL.MySQLServer = data.MySQLServer;
            MySQL.MySQLUser = data.MySQLUser;
            MySQL.MySQLDatabase = data.MySQLDatabase;
            MySQL.MySQLPort = data.MySQLPort;
            MySQL.MySQLPassword = data.MySQLPassword;
            GeniusAPIKey = data.GeniusAPIKey;
            LoggingLevel = data.LoggingLevel;
            Pythoninterpreter = data.Pythoninterpreter;
            SupportChannelId = data.SupportChannelId;
            SupportGuildId = data.SupportGuildId;

            MySQL.connStr = $"server={MySQL.MySQLServer};user={MySQL.MySQLUser};database={MySQL.MySQLDatabase};port={MySQL.MySQLPort};password={MySQL.MySQLPassword}";
        }

        public class JsonItems
        {
            public string Token { get; set; }
            public string Prefix { get; set; }
            public string Version { get; set; }
            public string YouTubeAPIKey { get; set; }
            public uint MaxUserPingCount { get; set; }
            public uint MaxRolePingCount { get; set; }
            public uint LevelMultiplier { get; set; }
            public uint MinMessageTimestamp { get; set; }
            public string GoogleSearchAPIKey { get; set; }
            public string MySQLServer { get; set; }
            public string MySQLUser { get; set; }
            public string MySQLDatabase { get; set; }
            public string MySQLPort { get; set; }
            public string MySQLPassword { get; set; }
            public string GeniusAPIKey { get; set; }
            public string LoggingLevel { get; set; }
            public string Pythoninterpreter { get; set; }
            public ulong SupportChannelId { get; set; }
            public ulong SupportGuildId { get; set; }
        }

        public static void ConsoleLog(string ConsoleMessage, ConsoleColor FColor = ConsoleColor.Green, ConsoleColor BColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = FColor;
            Console.BackgroundColor = BColor;
            Console.WriteLine("[ - Internal - ] - " + ConsoleMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static void SaveInfractionMessageCards()
        {
            string s = "";
            
            foreach (KeyValuePair<ulong, ulong> item in InfractionMessageHandler.CurrentInfractionMessages)
            {
                s += item.Key + "," + item.Value + "\n";
            }

            File.WriteAllText(infractionMessagefilepath, s);
        }

        public static Dictionary<ulong, ulong> LoadInfractionMessageCards()
        {
            string t = File.ReadAllText(infractionMessagefilepath);
            Dictionary<ulong, ulong> ulist = new Dictionary<ulong, ulong>();
          
            if (t == "")
            {
                return ulist;
            }

            foreach (string i in t.Split("\n"))
            {
                if (i != "")
                {
                    string[] spl = i.Split(",");
                    ulist.Add(ulong.Parse(spl[0]), ulong.Parse(spl[1]));
                }
            }

            return ulist;
        }

        public static void SaveCenssor(string word)
        {
            File.AppendAllText(CensoredWordsPath, $"{word}\n");
        }

        public static void SaveLeetRules(string leet, string value)
        {
            File.AppendAllText(LeetRulesPath, $"{leet}, {value}\n");
        }

        public static Dictionary<string, string> LoadLeetRules()
        {
            string t = File.ReadAllText(LeetRulesPath);
            Dictionary<string, string> list = new Dictionary<string, string>();
           
            if (t == "")
            {
                return list;
            }

            foreach (string i in t.Split("\n"))
            {
                if (i != "")
                {
                    string[] spl = i.Split(",");
                    list.Add(spl[0], spl[1]);
                }
            }

            return list;
        }

        public static void RemoveCensor(string word)
        {
            string[] oldLines = File.ReadAllLines(CensoredWordsPath);
            IEnumerable<string> newLines = oldLines.Where(line => !line.Contains(word));
            File.WriteAllLines(CensoredWordsPath, newLines);
        }

        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static bool IsDev(SocketUser user)
        {
            return DevUIDs.Contains(user.Id);
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, string newMessage)
        {
            await baseMessage.ModifyAsync(x => { x.Content = newMessage; });
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, EmbedBuilder embed)
        {
            await baseMessage.ModifyAsync(x => { x.Embed = embed.Build(); });
        }

        public class processes
        {
            public int MainBotPID { get; set; }
            public int PyBotPID { get; set; }
            public static int ProcessID { get; set; }
        }

        public static int GetPreviousProcessTaskPID()
        {
            string Data = File.ReadAllText(@$"{Environment.CurrentDirectory}\Data\PID.txt");

            if (string.IsNullOrEmpty(Data))
            {
                return 0;
            }

            else
            {
                return Convert.ToInt32(Data);
            }
        }

        public static void UpdatePIDValue(int PID)
        {
            File.WriteAllText(@$"{Environment.CurrentDirectory}\Data\PID.txt", $"{PID}");
        }    
    }
}
