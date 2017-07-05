using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace Youtube_DJ_Bot
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.Title = "Telegram Bot";

			Console.WriteLine("Creating the bot...");
			Bot bot = new Bot();

			Console.WriteLine("Fetching YouTube videos...");
			bot.FetchYoutubeFavorites();

			Console.WriteLine("Starting the bot...");
			bot.Start();

			Console.Title = bot.BotName;
			Console.WriteLine("At any time, press any alphanum key to close the bot.");
			Thread.Sleep(500);
			Console.WriteLine("The bot is now working.\n\n");
			MinimizeConsoleWindow();

			Console.Read();
			bot.Stop();
		}

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
	}
}
