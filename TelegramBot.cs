using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
	class TelegramBot
	{
		static string Token { get; set; }
		static string tokenFilePath = Environment.CurrentDirectory + "\\Token.txt";
		static ITelegramBotClient botClient;
		static CancellationTokenSource cts = new CancellationTokenSource();

		static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			try
			{
				if (update.Type == UpdateType.Message)
				{
					if (update.Message is not { } message)
						return;

					await OnMessage(message);
				}

				if (update.Type == UpdateType.CallbackQuery)
				{
					if (update.CallbackQuery is not { } query)
						return;

					await OnCallbackQuery(query);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			var ErrorMessage = exception switch
			{
				ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => exception.ToString()
			};

			Console.WriteLine(ErrorMessage);
			return Task.CompletedTask;
		}

		static async Task OnMessage(Message message)
		{
			if (message.Text is not { } messageText) return;

			if (message.From is not { } messageFrom) return;

			messageText = messageText.Trim();

			long userId = messageFrom.Id;
			long chatId = message.Chat.Id;

			Console.WriteLine($"Received a '{messageText}' message from {userId} in chat {chatId}.");

			if (messageText.StartsWith('/'))
			{
				await OnCommand(messageText.ToLower(), chatId, userId);
			}
			else
			{
				await OnTextMessage(messageText, chatId, userId);
			}
		}

		static async Task OnCommand(string messageText, long chatId, long userId)
		{
			switch (messageText)
			{
				case "/stop":
					cts.Cancel();
					Environment.Exit(0);
					break;
				case "/restart":
					cts.Cancel();
					Process.Start(Environment.ProcessPath!);
					Environment.Exit(0);
					break;
			}

			ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
					{
						new KeyboardButton[] { "🐈 Кошка", "🐕 Собака", "🦖 Динозавр", "🐸 Жаба" },
					})
			{
				ResizeKeyboard = true,
				OneTimeKeyboard = true
			};

			bool userAlreadyExists = await DbMethods.GetUserId(userId) > 0;

			switch (messageText)
			{
				case "/start":
					await botClient.SendTextMessageAsync(chatId, """
                        /start - Показывает это сообщение
                        /reg - Регистрирует Вас в боте
                        /rereg - Перерегистрирует Вас в боте
                        /info - Показывает информацию о вашем питомце
                        /stop - Выключает бота
                        /restart - Перезагружает бота
                        """, cancellationToken: cts.Token);
					break;
				case "/reg":
					if (userAlreadyExists) { await botClient.SendTextMessageAsync(chatId, "Вы уже зарегистрированы!", cancellationToken: cts.Token); break; }
					await DbMethods.RegisterUser(userId);
					await botClient.SendTextMessageAsync(chatId, "Выберите питомца:", replyMarkup: replyKeyboardMarkup, cancellationToken: cts.Token);
					break;
				case "/rereg":
					if (!userAlreadyExists) { await botClient.SendTextMessageAsync(chatId, "Вы ещё не зарегистрированы!", cancellationToken: cts.Token); break; }
					await DbMethods.ChangeUserRegState(userId);
					await botClient.SendTextMessageAsync(chatId, "Выберите питомца:", replyMarkup: replyKeyboardMarkup, cancellationToken: cts.Token);
					break;
				case "/info":
					string petName = await DbMethods.GetUserPet(userId);
					await botClient.SendTextMessageAsync(chatId, $"Ваш питомец — {petName}", cancellationToken: cts.Token);
					break;
				default:
					await botClient.SendTextMessageAsync(chatId, "Неизвестная команда", cancellationToken: cts.Token);
					break;
			}
		}

		static async Task OnTextMessage(string messageText, long chatId, long userId)
		{
			int petId;

			switch (messageText)
			{
				case "🐈 Кошка":
					petId = 1;
					break;
				case "🐕 Собака":
					petId = 2;
					break;
				case "🦖 Динозавр":
					petId = 3;
					break;
				case "🐸 Жаба":
					petId = 4;
					break;
				default:
					await botClient.SendTextMessageAsync(chatId, "Неизвестная команда", cancellationToken: cts.Token);
					return;
			}

			int result = await DbMethods.SetUserPet(userId, petId);

			if (result > 0)
			{
				await botClient.SendTextMessageAsync(chatId, "Успешная регистрация / перерегистрация!", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cts.Token);
			}
			else
			{
				await botClient.SendTextMessageAsync(chatId, "Вы ещё не зарегистрированы, либо не начат процесс перерегистрации.", cancellationToken: cts.Token);
			}
		}

		static async Task OnCallbackQuery(CallbackQuery callbackQuery)
		{
			return;
		}

		static async Task Main(string[] args)
		{
			if (args.Length > 0)
			{
				Token = args[0];
			}
			else if (System.IO.File.Exists(tokenFilePath)
				&& new FileInfo(tokenFilePath).Length != 0)
			{
				Token = System.IO.File.ReadAllText(tokenFilePath);
			}
			else
			{
				Console.WriteLine("No tg bot token was provided!");
				Console.ReadKey();
				return;
			}

			botClient = new TelegramBotClient(Token);

			ReceiverOptions receiverOptions = new() { AllowedUpdates = { }, ThrowPendingUpdates = true };

			botClient.StartReceiving(
				updateHandler: HandleUpdateAsync,
				pollingErrorHandler: HandleErrorAsync,
				receiverOptions: receiverOptions,
				cancellationToken: cts.Token);

			User me = await botClient.GetMeAsync();

			Console.WriteLine($"Start listening for @{me.Username}");
			Console.ReadLine();

			cts.Cancel();
		}
	}
}