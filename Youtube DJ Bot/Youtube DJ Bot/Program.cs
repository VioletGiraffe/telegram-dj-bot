using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;


namespace Youtube_DJ_Bot
{
    internal class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient("413647505:AAFyR0CDTYzlnuGYt2soqdi5JXKCBSFvG50");

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

        static void Main(string[] args)
        {
            Program p = new Program();
            Console.Title = "Fetching YouTube videos...";
            p.FetchYtUploads().Wait();

            MinimizeConsoleWindow();

            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += p.BotOnMessageReceived;
            Bot.OnMessageEdited += p.BotOnMessageReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            var me = Bot.GetMeAsync().Result;

            Console.Title = me.Username;

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private List<string> _favoriteVideoUrls = new List<string>();
        private Random _rng = new Random();

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("BotOnReceiveError: " + receiveErrorEventArgs.ApiRequestException.Message);
            Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            InlineQueryResult[] results = {
                new InlineQueryResultVideo
                {

                }
            };

            await Bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;

            if (message.Text.StartsWith("/song")) // request a random song
            {
                if (_favoriteVideoUrls.Count == 0)
                    return;

                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await Bot.SendTextMessageAsync(message.Chat.Id, _favoriteVideoUrls[_rng.Next(0, _favoriteVideoUrls.Count)], replyMarkup: new ReplyKeyboardHide());
            }
            else
            {
                var usage = @"Usage:
/song or > - post a random song
/song part_of_the_title_name - list all songs from the playlist that contain part_of_the_title_name (case insensitive)
";

                await Bot.SendTextMessageAsync(message.Chat.Id, usage,
                    replyMarkup: new ReplyKeyboardHide());
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }

        private async Task FetchYtUploads()
        {
            UserCredential credential;
            var credentialsPath = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase), @"..\..\..\..\client_id.json").Replace("file:\\", "");
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for read-only access to the authenticated 
                    // user's account, but not other types of account access.
                    new[] { YouTubeService.Scope.YoutubeReadonly },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            var channelsListRequest = youtubeService.Channels.List("contentDetails");
            channelsListRequest.Mine = true;
            var channels = await channelsListRequest.ExecuteAsync();

            if (channels.Items.Count <= 0)
            {
                Console.Write("ERROR: no YT channels found.");
                return;
            }

            var favoritesListId = channels.Items[0].ContentDetails.RelatedPlaylists.Favorites;

            string nextPageToken = "";
            var favoriteVideos = new List<PlaylistItem>();
            while (nextPageToken != null)
            {
                var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = favoritesListId;
                playlistItemsListRequest.MaxResults = 50;
                playlistItemsListRequest.PageToken = nextPageToken;

                var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                favoriteVideos.AddRange(playlistItemsListResponse.Items);
                nextPageToken = playlistItemsListResponse.NextPageToken;
            }

            for (int i = 0; i < favoriteVideos.Count; ++i)
            {
                var item = favoriteVideos[i];
                Console.WriteLine((i + 1).ToString() + ": " + item.Snippet.Title);
                if (item.Snippet.Title != "Deleted video" && item.Snippet.Title != "Private video")
                    _favoriteVideoUrls.Add("https://youtu.be/" + item.Snippet.ResourceId.VideoId);
            }
        }
    }
}
