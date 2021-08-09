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
            new Program().InitMainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User)
                return;

            if(message.Content.Contains("steam"))
            {
                //We're specifically searching for domain names that have the word steam in it
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
                        var context = new SocketCommandContext(_client, message);
                        await context.Guild.AddBanAsync(context.User, 1, "Scammer Bot");
                    }

                }
            }
        }

        private Task LogAsync(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
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
