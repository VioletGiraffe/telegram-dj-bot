using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot.Types;

namespace Youtube_DJ_Bot
{
	class Bot
	{
		TelegramClient _client = null;
		private static YouTubeClient _youTube = null;
		private static Random _rng = new Random();

		public void FetchYoutubeFavorites()
		{
			_youTube = new YouTubeClient();
			_youTube.GetFavoriteVideos();
		}

		public void Start()
		{
			_client = new TelegramClient();

			_client.MessageReceived += OnMessageReceived;

			_client.Start();
		}

		public void Stop()
		{
			_client.Stop();
		}

		public string BotName
		{
			get { return _client.Me.Username; }
		}

		private void OnMessageReceived(Message msg)
		{
			if (msg.Text.StartsWith("/song") || msg.Text == ">") // request a random song
			{
				if (_youTube.Favorites.Count == 0)
					return;

				string reply = _youTube.Favorites[_rng.Next(0, _youTube.Favorites.Count)];
				Console.WriteLine(UsernameFromMessage(msg) + " requests a song. Replying with\n" + reply + "\n");
				_client.SendReply(reply, msg.Chat.Id);
			}
			else
			{
				var usage = @"Usage:
/song or > - post a random song
/song part_of_the_title_name - list all songs from the playlist that contain part_of_the_title_name (case insensitive)
";
				_client.SendReply(usage, msg.Chat.Id);
			}
		}

		private static string UsernameFromMessage(Message msg)
		{
			string name = "";
			if (msg.Chat.FirstName != null)
				name += msg.Chat.FirstName;

			if (msg.Chat.LastName != null)
				name += " " + msg.Chat.LastName;

			string result = msg.Chat.Username != null ? msg.Chat.Username + "(" + name + ")" : name;
			return result;
		}
	}
}
