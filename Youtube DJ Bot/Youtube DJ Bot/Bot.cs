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
			var chatId = msg.Chat.Id;

			if (msg.Text.StartsWith("/song") || msg.Text == ">") // request a random song
			{
				if (_youTube.Favorites.Count == 0)
					return;

				string reply = _youTube.Favorites[_rng.Next(0, _youTube.Favorites.Count)].Url();
				Console.WriteLine(UsernameFromMessage(msg) + " requests a song. Replying with\n" + reply + "\n");
				_client.SendReply(reply, chatId);
			}
			else if (msg.Text.StartsWith("/find "))
			{
				string subject = msg.Text.Substring("/find ".Length).ToLower();
				bool searchInDescription = subject.StartsWith("-d ");
				if (searchInDescription)
					subject = subject.Substring("-d ".Length);

				if (string.IsNullOrEmpty(subject))
					return;

				Console.WriteLine(UsernameFromMessage(msg) + " is searching for\n" + subject);

				foreach (var vid in _youTube.Favorites)
				{
					if (vid.Title.ToLower().Contains(subject) || (searchInDescription && vid.Description.ToLower().Contains(subject)))
					{
						Console.WriteLine("Match: " + vid.Url());
						_client.SendReply(vid.Url(), chatId);
					}
				}

				Console.WriteLine("Search completed.");
			}
			else
			{
				var usage = @"Usage:
/song or > - post a random song
/find [-d] subject - list all songs from the playlist that contain subject in the title, or description if -d is specified (case insensitive)
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
