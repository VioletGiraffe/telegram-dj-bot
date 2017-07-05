using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace Youtube_DJ_Bot
{
	class TelegramClient
	{
		private readonly TelegramBotClient _bot = new TelegramBotClient("413647505:AAFyR0CDTYzlnuGYt2soqdi5JXKCBSFvG50");
		private User _me = null;

		public delegate void TelegramMessagehandler(Message msg);
		public event TelegramMessagehandler MessageReceived;

		public TelegramClient()
		{
			Init();
		}

		public void Start()
		{
			_bot.StartReceiving();
		}

		public void Stop()
		{
			_bot.StopReceiving();
		}

		public void Init()
		{
			_bot.OnCallbackQuery += BotOnCallbackQueryReceived;
			_bot.OnMessage += BotOnMessageReceived;
			_bot.OnMessageEdited += BotOnMessageReceived;
			_bot.OnInlineQuery += BotOnInlineQueryReceived;
			_bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
			_bot.OnReceiveError += BotOnReceiveError;

			_me = _bot.GetMeAsync().Result;
		}

		public User Me
		{
			get { return _me; }
		}

		public async void SendReply(string text, long chatId)
		{
			if (string.IsNullOrEmpty(text))
				return;

			//await _bot.SendChatActionAsync(chatId, ChatAction.Typing);
			await _bot.SendTextMessageAsync(chatId, text, replyMarkup: new ReplyKeyboardHide());
		}

		private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
		{
			Console.WriteLine("BotOnReceiveError: " + receiveErrorEventArgs.ApiRequestException.Message);
			Debugger.Break();
		}

		private void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
		{
			Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
		}

		private async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
		{
			InlineQueryResult[] results = {
				new InlineQueryResultVideo
				{

				}
			};

			await _bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
		}

		private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
		{
			var message = messageEventArgs.Message;

			if (message == null || message.Type != MessageType.TextMessage) return;

			if (MessageReceived != null)
				MessageReceived(message);
		}

		private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
		{
			await _bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
				$"Received {callbackQueryEventArgs.CallbackQuery.Data}");
		}
	}
}
