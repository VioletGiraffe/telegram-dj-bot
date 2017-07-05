using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Telegram.Bot.Types;

namespace Youtube_DJ_Bot
{
	internal class Program
	{
		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern IntPtr GetConsoleWindow();

		[DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ShowWindow([In] IntPtr hWnd, [In] Int32 nCmdShow);

		private static void MinimizeConsoleWindow()
		{
			const Int32 SW_MINIMIZE = 6;
			IntPtr hWndConsole = GetConsoleWindow();
			if (hWndConsole != null)
				ShowWindow(hWndConsole, SW_MINIMIZE);
		}

		static TelegramClient _bot = null;
		private static YouTubeClient youTube = null;
		private static Random _rng = new Random();

		static void Main(string[] args)
		{
			Console.Title = "Fetching YouTube videos...";
			youTube = new YouTubeClient();
			youTube.GetFavoriteVideos();

			_bot = new TelegramClient();
			Console.Title = _bot.Me.Username;
			_bot.MessageReceived += OnMessageReceived;

			MinimizeConsoleWindow();

			_bot.Start();
			Console.ReadLine();
			_bot.Stop();
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

		private static void OnMessageReceived(Message msg)
		{
			if (msg.Text.StartsWith("/song") || msg.Text == ">") // request a random song
			{
				if (youTube.Favorites.Count == 0)
					return;

				string reply = youTube.Favorites[_rng.Next(0, youTube.Favorites.Count)];
				Console.WriteLine(UsernameFromMessage(msg) + " requests a song. Replying with\n" + reply + "\n");
				_bot.SendReply(reply, msg.Chat.Id);
			}
			else
			{
				var usage = @"Usage:
/song or > - post a random song
/song part_of_the_title_name - list all songs from the playlist that contain part_of_the_title_name (case insensitive)
";
				_bot.SendReply(usage, msg.Chat.Id);
			}
		}
	}
}
