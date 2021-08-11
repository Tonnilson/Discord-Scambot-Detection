using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord_ScamBot_Detection
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly bool _deleteMessageOnly;
        private string _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt");

        private List<string> validSteamUrls = new List<string> { 
            "steamcommunity", 
            "www.steamcommunity", 
            "steampowered",
            "www.steampowered",
            "store.steampowered",
            "cdn.akamai.steamstatic"
        };

        static void Main(string[] args)
        {
            new Program(args).InitMainAsync().GetAwaiter().GetResult();
        }

        public Program(string[] args)
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;

            if (args.Length > 0)
                _deleteMessageOnly = args[0].Contains("-deleteMessageOnly", StringComparison.OrdinalIgnoreCase);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User)
                return;

            if(message.Content.Contains("steam"))
            {
                /// Regex search for http url link with the word 'steam'
                /// Ignore cases can be handled in Regex but I do not want to do that because regex is costly as is
                /// (http|https):\/\/(?!steamcommunity.com|www.steamcommunity.com|.*steampowered.com|.*steamstatic.com)(.*steam.*)\.([a-zA-Z]{2,})
                Regex patterrn = new Regex(@"(http|https):\/\/(?<Domain>.*steam?.*)\.(?<ext>[a-zA-Z]{2,})");
                var result = patterrn.Match(message.Content);
                //Domain with the word 'steam' detected
                if (result.Success)
                {
                    //Check for inconsistency
                    if (result.Groups["ext"].Value == "com" && validSteamUrls.Contains(result.Groups["Domain"].Value, StringComparer.OrdinalIgnoreCase))
                        return; //Valid steamcommunity url
                    else
                    {
                        await LogAsync(new LogMessage(LogSeverity.Info, "Scam Detection", String.Format("{0} ({1}): {2}", message.Author.Username, message.Author.Id, message.Content)));
                        
                        if (_deleteMessageOnly)
                            await message.DeleteAsync();
                        else
                        {
                            var context = new SocketCommandContext(_client, message);
                            await context.Guild.AddBanAsync(context.User, 1, "Scammer Bot"); //User ID, Amount of days to prune meesage(s), Reason
                        }
                    }

                }
            }
        }

        private Task LogAsync(LogMessage arg)
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFile))
                File.Create(_logFile).Dispose();

            string logText = $"{DateTime.Now.ToString("hh:mm:ss")} [{arg.Severity}] {arg.Source}: {arg.Exception?.ToString() ?? arg.Message}";
            File.AppendAllTextAsync(_logFile, logText + "\n");

            return Console.Out.WriteLineAsync(logText);
        }

        public async Task InitMainAsync()
        {
            if(!File.Exists("discord.token"))
            {
                Console.WriteLine("The file: discord.token is missing, please create this and put your bot token in to continue");
                Console.ReadKey();
                return;
            }

            await _client.LoginAsync(TokenType.Bot, File.ReadAllText("discord.token")); //Read the file for the token
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }
    }
}
