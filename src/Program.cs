using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace LinearsBot
{
    public class Program
    {
		private readonly ushort[] version = new ushort[3]{ 1, 2, 11 }; // Major, Minor, Patch
		private WebClient webClient;

		static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			if (!Directory.Exists("data"))
				Directory.CreateDirectory("data");

			if (!File.Exists("data/stafflist"))
				File.Create("data/stafflist");

			if (!File.Exists("data/serverslist"))
				File.Create("data/serverslist");

			JObject config = Functions.GetConfig();
			if (config["keepUpdated"].Value<bool>())
			{
				webClient = new WebClient();
				Uri webVersion = new Uri("https://raw.githubusercontent.com/Cu-chi/linears-utils/master/version");
				webClient.DownloadStringAsync(webVersion);
				webClient.DownloadStringCompleted += DownloadStringVersionCompleted;
			}

			using var services = ConfigureServices();

			Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Yellow, "-> Le bot démarre...");
			DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

			client.Log += Log;
			services.GetRequiredService<CommandService>().Log += Log;

			string token = config["token"].Value<string>();
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
			await Task.Delay(-1);
		}

		private void DownloadStringVersionCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			ushort[] versionInformations = new ushort[3]{
				Convert.ToUInt16(e.Result.Split('.')[0]), // Major
				Convert.ToUInt16(e.Result.Split('.')[1]), // Minor
				Convert.ToUInt16(e.Result.Split('.')[2])  // Patch
			};

			if (versionInformations[0] > version[0] || versionInformations[1] > version[1] || versionInformations[2] > version[2])
			{
				Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Red, "-> Une nouvelle version est disponible :");
				Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Yellow, $"-> Version locale : {version[0]}.{version[1]}.{version[2]}");
				Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Green, $"-> Version distante : {versionInformations[0]}.{versionInformations[1]}.{versionInformations[2]}");
				Console.WriteLine();
				Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Magenta, "-> La version va être télécharhée vers \"\"");

				Uri fileLink = new Uri($"https://github.com/Cu-chi/linears-utils/releases/download/{versionInformations[0]}.{versionInformations[1]}.{versionInformations[2]}/linearsbot-{versionInformations[0]}.{versionInformations[1]}.{versionInformations[2]}.zip");

				webClient.DownloadFileAsync(fileLink, "new-linears-bot.zip");
				webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
			}
		}

		private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Cancelled || e.Error != null)
			{
				Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Red, "-> Le téléchargement n'a pas pu se finir, essayez de l'effectuer manuellement en se rendant sur le lien du projet github :\n-> https://github.com/Cu-chi/linears-utils/releases");
				return;
			}
			Directory.CreateDirectory("lastupdate");
			ZipFile.ExtractToDirectory("new-linears-bot.zip", "lastupdate");
			Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Green, "-> Nouvelle version téléchargée avec succès vers :\n-> " + Directory.GetCurrentDirectory() + "\\lastupdate");
		}

		public ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
				.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
				{
					AlwaysDownloadUsers = true,
					MessageCacheSize = 100,
					GatewayIntents = GatewayIntents.All,
					LogLevel = LogSeverity.Verbose
				}))
				.AddSingleton(new CommandService(new CommandServiceConfig
                { 
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = RunMode.Async,
                    CaseSensitiveCommands = false,
                }))
                .AddSingleton<CommandHandlingService>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
