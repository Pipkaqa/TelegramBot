#pragma warning disable CS8604, CS8600, CS8622, CS8602

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfosphereBot
{
    public class Bot
    {
        public Bot(string token)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(BotStatics.OnExit);

            Client = new(token);

            Client.StartReceiving(HandleUpdateAsync, HandleError, ReceiverOptions, CancellationTokenSource.Token);

            StartClearingLastUpdates(BotStatics.UpdatesClearingDelayInSeconds);

            StartClearingLastSpammers(BotStatics.SpammersClearingDelayInSeconds);

            ConsoleStatics.CreateLog("Bot started...", ConsoleColor.Blue);

            ConsoleStatics.HandleConsoleCommands(Client, BotStatics.Admins, BotStatics.BannedUsers);
        }

        ~Bot()
        {
            CancellationTokenSource.Cancel();
        }

        public readonly TelegramBotClient Client;
        public readonly ReceiverOptions ReceiverOptions = new();
        public readonly CancellationTokenSource CancellationTokenSource = new();

        private readonly List<long> _lastUpdates = new(25);
        private readonly Dictionary<long, int> _spammers = new(25);

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            long instigatorId;

            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message == null) { return; }

                    instigatorId = update.Message.Chat.Id;

                    if (BotStatics.IsBanned(instigatorId, update.Message.Chat.FirstName, update.Message.Text))
                    {
                        return;
                    }

                    if (!await BotStatics.Admins.ContainsAsync(update.Message.Chat.Id) && IsSpam(update.Message.Chat))
                    {
                        ConsoleStatics.CreateLog($"Probably, spam detected!\nName: {update.Message.Chat.FirstName} | ID: {instigatorId}", ConsoleColor.Yellow);

                        await TrySendTextMessageAsync(instigatorId, "Вы отправляете запросы слишком часто, пожалуйста, перестаньте или нам придётся игнорировать вас.");

                        return;
                    }

                    await OnMessageReceivedAsync(client, update.Message);

                    break;

                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery == null) { return; }

                    instigatorId = update.CallbackQuery.Message.Chat.Id;

                    if (BotStatics.IsBanned(instigatorId, update.CallbackQuery.Message.Chat.FirstName, update.CallbackQuery.Message.Text))
                    {
                        return;
                    }

                    if (!await BotStatics.Admins.ContainsAsync(update.CallbackQuery.Message.Chat.Id) && IsSpam(update.CallbackQuery.Message.Chat))
                    {
                        ConsoleStatics.CreateLog($"Probably, spam detected!\nName: {update.CallbackQuery.Message.Chat.FirstName} | ID: {instigatorId}", ConsoleColor.Yellow);

                        await TrySendTextMessageAsync(instigatorId, "Вы отправляете запросы слишком часто, пожалуйста, перестаньте или нам придётся игнорировать вас.");

                        return;
                    }

                    await OnCallbackQueryReceivedAsync(client, update.CallbackQuery);

                    break;
            }
        }

        private Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            string errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram Bot API Error:\nError code: {apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => $"Fatal Error:\n{exception}"
            };

            ConsoleStatics.CreateLog(errorMessage, ConsoleColor.Red);

            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(ITelegramBotClient client, Message message)
        {
            long instigatorId = message.Chat.Id;

            switch (message.Type)
            {
                case MessageType.Text:
                    if (message.Text[0] == '/')
                    {
                        message.Text = message.Text.ToLower();
                        await HandleCommandAsync(client, message);
                        break;
                    }

                    await HandleTextAsync(client, message);
                    break;

                default:
                    ConsoleStatics.CreateLog($"Received unknown message!\nName: {message.Chat.FirstName} | ID: {message.Chat.Id}");
                    await TrySendTextMessageAsync(instigatorId, BotStatics.UnknownCommandMessage);
                    break;
            }
        }

        private async Task OnCallbackQueryReceivedAsync(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            callbackQuery.Data = callbackQuery.Data.ToLower();

            if (callbackQuery.Data[0] == '!')
            {
                await HandleAdminQueryAsync(client, callbackQuery);
                return;
            }

            await HandleUserQueryAsync(client, callbackQuery);
        }

        private async Task HandleTextAsync(ITelegramBotClient client, Message message)
        {
            ConsoleStatics.CreateLog($"Received new text message!\nName: {message.Chat.FirstName} | ID: {message.Chat.Id} | Text: {message.Text}");

            long instigatorId = message.Chat.Id;

            if (message.Text.ToLower() == "привет")
            {
                InlineKeyboardMarkup keyboard = new(new[]
{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("О направлениях", "start_1")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Записаться на пробное занятие", BotStatics.Url)
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Часто задаваемые вопросы", "start_3")
                    },
                });

                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), replyMarkup: keyboard,
                    caption: "Привет, это компьютерная школа \"Инфосфера\"!)\n\n" +
                             "В нашей школе дети осваивают компьютерные технологии на профессиональном уровне. " +
                             "Обучение в школе поможет воспитать у детей информационную культуру, развить системное мышление и научиться решать задачи творчески.\n\n" +
                             "Сейчас успешно обучаются более 600 детей от 6 до 12 лет по следующим направлениям:\n\n" +
                             "● Информатика\n" +
                             "● Информационные технологии\n" +
                             "● Робототехника\n" +
                             "● Программирование\n" +
                             "● Компьютерная графика\n\n" +
                             "Квалифицированные педагоги-практики помогут ребятам реализовать творческий потенциал и воплотить в жизнь свои уникальные идеи и замыслы!");

                stream.Close();
            }
            else if (message.ReplyToMessage != null && message.ReplyToMessage.From.IsBot)
            {
                if (message.ReplyToMessage.Text == "Напишите ID пользователя, которого хотите забанить.")
                {
                    if (message.Text.Length <= 0 || message.Text == null || message.Text == "")
                    {
                        ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                        await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                        return;
                    }

                    foreach (char c in message.Text)
                    {
                        if (!char.IsDigit(c))
                        {
                            ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                            await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                            return;
                        }
                    }

                    long targetId = Convert.ToInt64(message.Text);

                    Task<Chat> task = client.GetChatAsync(targetId);

                    try
                    {
                        task.Wait();

                        Chat user = task.Result;

                        if (!await BotStatics.BannedUsers.AddAsync(new User(targetId, user.FirstName, user.LastName, user.Username)))
                        {
                            ConsoleStatics.CreateLog($"User already banned!\nID: {targetId}", ConsoleColor.Blue);

                            await TrySendTextMessageAsync(instigatorId, "Пользователь уже забанен!");

                            return;
                        }

                        ConsoleStatics.CreateLog($"User has been banned!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь был забанен!");
                    }
                    catch
                    {
                        ConsoleStatics.CreateLog($"User doesn't exist!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь не существует!");
                    }
                }
                else if (message.ReplyToMessage.Text == "Напишите ID пользователя, которого хотите разбанить.")
                {
                    if (message.Text.Length <= 0 || message.Text == null || message.Text == "")
                    {
                        ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                        await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                        return;
                    }

                    foreach (char c in message.Text)
                    {
                        if (!char.IsDigit(c))
                        {
                            ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                            await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                            return;
                        }
                    }

                    long targetId = Convert.ToInt64(message.Text);

                    Task<Chat> task = client.GetChatAsync(targetId);

                    try
                    {
                        task.Wait();

                        Chat user = task.Result;

                        if (!await BotStatics.BannedUsers.RemoveAsync(targetId))
                        {
                            ConsoleStatics.CreateLog($"User is not banned!\nID: {targetId}", ConsoleColor.Blue);

                            await TrySendTextMessageAsync(instigatorId, "Пользователь не является забаненным!");

                            return;
                        }

                        ConsoleStatics.CreateLog($"User has been unbanned!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь был разбанен!");
                    }
                    catch
                    {
                        ConsoleStatics.CreateLog($"User doesn't exist!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь не существует!");
                    }
                }
                else if (message.ReplyToMessage.Text == "Напишите ID пользователя, которого хотите сделать администратором.")
                {
                    if (message.Text.Length <= 0 || message.Text == null || message.Text == "")
                    {
                        ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                        await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                        return;
                    }

                    foreach (char c in message.Text)
                    {
                        if (!char.IsDigit(c))
                        {
                            ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                            await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                            return;
                        }
                    }

                    long targetId = Convert.ToInt64(message.Text);

                    Task<Chat> task = client.GetChatAsync(targetId);

                    try
                    {
                        task.Wait();

                        Chat user = task.Result;

                        if (!BotStatics.Admins.Add(new User(targetId, user.FirstName, user.LastName, user.Username)))
                        {
                            ConsoleStatics.CreateLog($"User already is admin!\nID: {targetId}", ConsoleColor.Blue);

                            await TrySendTextMessageAsync(instigatorId, "Пользователь уже является администратором!");

                            return;
                        }

                        ConsoleStatics.CreateLog($"User has been promoted to admin!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь получил права администратора!");
                    }
                    catch
                    {
                        ConsoleStatics.CreateLog($"User doesn't exist!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь не существует!");
                    }
                }
                else if (message.ReplyToMessage.Text == "Напишите ID пользователя, которого хотите лишить привелегий администратора.")
                {
                    if (message.Text.Length <= 0 || message.Text == null || message.Text == "")
                    {
                        ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                        await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                        return;
                    }

                    foreach (char c in message.Text)
                    {
                        if (!char.IsDigit(c))
                        {
                            ConsoleStatics.CreateLog(ConsoleStatics.WrongArgumentsError, ConsoleColor.Red);

                            await TrySendTextMessageAsync(instigatorId, ConsoleStatics.WrongArgumentsError);

                            return;
                        }
                    }

                    long targetId = Convert.ToInt64(message.Text);

                    if (!BotStatics.Admins.Remove(targetId))
                    {
                        ConsoleStatics.CreateLog($"User is not an admin!\nID: {targetId}", ConsoleColor.Blue);

                        await TrySendTextMessageAsync(instigatorId, "Пользователь не является администратором!");

                        return;
                    }

                    ConsoleStatics.CreateLog($"User has been demoted from admin!\nID: {targetId}", ConsoleColor.Blue);

                    await TrySendTextMessageAsync(instigatorId, "Пользователь лишён прав администратора!");
                }
            }
            else
            {
                await TrySendTextMessageAsync(instigatorId, BotStatics.UnknownCommandMessage);
            }
        }

        private async Task HandleCommandAsync(ITelegramBotClient client, Message message)
        {
            long instigatorId = message.Chat.Id;

            ConsoleStatics.CreateLog($"Received new command message!\nName: {message.Chat.FirstName} | ID: {instigatorId} | Command: {message.Text}");

            if (message.Text == "/start")
            {
                InlineKeyboardMarkup keyboard = new(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("О направлениях", "start_1")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Записаться на пробное занятие", BotStatics.Url)
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Часто задаваемые вопросы", "start_3")
                    },
                });

                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), replyMarkup: keyboard,
                    caption: "Привет, это компьютерная школа \"Инфосфера\"!)\n\n" +
                             "В нашей школе дети осваивают компьютерные технологии на профессиональном уровне. " +
                             "Обучение в школе поможет воспитать у детей информационную культуру, развить системное мышление и научиться решать задачи творчески.\n\n" +
                             "Сейчас успешно обучаются более 600 детей от 6 до 12 лет по следующим направлениям:\n\n" +
                             "● Информатика\n" +
                             "● Информационные технологии\n" +
                             "● Робототехника\n" +
                             "● Программирование\n" +
                             "● Компьютерная графика\n\n" +
                             "Квалифицированные педагоги-практики помогут ребятам реализовать творческий потенциал и воплотить в жизнь свои уникальные идеи и замыслы!");

                stream.Close();
            }
            else if (message.Text == "/console" && await BotStatics.Admins.ContainsAsync(message.Chat.Id))
            {
                InlineKeyboardMarkup keyboard = new(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Забанить по ID", "!ban")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Разбанить по ID", "!unban")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Сделать админом по ID", "!make-an-admin")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Сделать пользователем по ID", "!make-a-user")
                    },
                });

                await TrySendTextMessageAsync(instigatorId, "Выбирайте функцию:", replyMarkup: keyboard);
            }
            else
            {
                await TrySendTextMessageAsync(instigatorId, BotStatics.UnknownCommandMessage);
            }
        }

        private async Task HandleUserQueryAsync(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            long instigatorId = callbackQuery.Message.Chat.Id;

            ConsoleStatics.CreateLog($"Received new user query!\nName: {callbackQuery.Message.Chat.FirstName} | ID: {instigatorId} | Callback: {callbackQuery.Data}");

            if (callbackQuery.Data == "start_1")
            {
                InlineKeyboardMarkup keyboard = new(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Подготовительная (1 класс)", "start_1_1")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Байтик (2 класс)", "start_1_2")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Инфомиры (3-4 класс)", "start_1_3")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Инфостарт (5-7 класс)", "start_1_4")
                    },
                });

                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start_1.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), caption: "Какую траекторию Вы хотите изучить:\n" +
                                                                                                 "● Подготовительная\n" +
                                                                                                 "● Байтик\n" +
                                                                                                 "● Инфомиры\n" +
                                                                                                 "● Инфостарт", replyMarkup: keyboard);

                stream.Close();
            }
            else if (callbackQuery.Data == "start_3")
            {
                InlineKeyboardMarkup keyboard = new(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Принимаете ли в школу 6-леток?", "start_3_1")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Что необходимо для учёбы в школе?", "start_3_2")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Нужен ли дома компьютер?", "start_3_3")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Режим обучения", "start_3_4")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Другой вопрос", "start_3_5")
                    }
                });

                await TrySendTextMessageAsync(instigatorId, "Выберите, интересующий Вас вопрос:", replyMarkup: keyboard);
            }
            else if (callbackQuery.Data == "start_3_1")
            {
                await TrySendTextMessageAsync(instigatorId, "Да, принимаем, но при прохождении тестирования.");
            }
            else if (callbackQuery.Data == "start_3_2")
            {
                await TrySendTextMessageAsync(instigatorId, "Ручка, карандаш, ластик, папка-скоросшиватель, флеш - накопитель. И желание учиться. :)");
            }
            else if (callbackQuery.Data == "start_3_3")
            {
                await TrySendTextMessageAsync(instigatorId, "Для выполнения домашних заданий компьютер будет необходим на ступени обучения «Инфомиры». " +
                                                                "Если дома компьютер отсутствует либо сломался, то ребенок может приходить в компьютерную школу «Инфосфера» и заниматься за свободным компьютером.");
            }
            else if (callbackQuery.Data == "start_3_4")
            {
                await TrySendTextMessageAsync(instigatorId, "Расписание компьютерной школы составлено таким образом, что дети приходят в Инфосферу 2-3 раза в неделю в зависимости от возраста и ступени обучения. " +
                                                                "Длительность занятий – 45 минут. 10 минут перемена. " +
                                                                "Занятия в компьютерной школе «Инфосфера» проводятся и в первую, и во вторую смену.");
            }
            else if (callbackQuery.Data == "start_3_5")
            {
                await TrySendTextMessageAsync(instigatorId, "Вы можете задать любой интересующий Вас вопрос по телефону: +7 (904) 056-72-88");
            }
            else if (callbackQuery.Data == "start_1_1")
            {
                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start_1_1.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), caption: "Развитие логического и образного мышления.\n\n" +
                                                                                                 "Творчество в мультимедийных адаптированных программах (Музыкальный конструктор MAGIX MUSIC MAKER, Audacity, Фантазеры), знакомство с компьютером, работа с микрофоном и устройствами ввода текстовой информации, конструирование");

                stream.Close();
            }
            else if (callbackQuery.Data == "start_1_2")
            {
                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start_1_2.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), caption: "Знакомство с компьютером и робототехникой.\n\n" +
                                                                                                 "Робототехника, создание мультфильмов с озвучкой в команде, изучение приемов форматирования текста в простых текстовых редакторах, работа в графическом редакторе Paint.");

                stream.Close();
            }
            else if (callbackQuery.Data == "start_1_3")
            {
                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start_1_3.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), caption: "Продвинутые навыки работы в Microsoft Office, основы программирования и робототехники.\n" +
                                                                                                 "Длится ступень 3 года: Инфомиры-1, Инфомиры-2, Инфомиры-3\n\n" +
                                                                                                 "Изучение устройств компьютера и организации файлового пространства, освоение основных алгоритмических конструкций, программирования и конструирования роботов, создание профессиональных презентаций и текстовых документов.");

                stream.Close();
            }
            else if (callbackQuery.Data == "start_1_4")
            {
                await using FileStream stream = new(Path.Combine(BotStatics.PicturesFolderPath, "start_1_4.jpg"), FileMode.Open);

                await TrySendPhotoAsync(instigatorId, InputFile.FromStream(stream), caption: "● Инфостарт-0 (5 класс)\n\n" +
                                                                                                 "Поступают ребята, которые до этого у нас не учились.\n" +
                                                                                                 "На этом курсе дети познакомятся с работой в графическом редакторе Figma, попробуют сверстать сайты, научатся конструировать роботов и программировать их.\n\n" +
                                                                                                 "Модульный интегрированный курс «Инфостарт-0» поможет тем, кто только что познакомился с Инфосферой, определится с интересами в IT - области и направлением дальнейшего обучения.\n\n" +
                                                                                                 "● Инфостарт (6, 7 класс)\n\n" +
                                                                                                 "Первое знакомство с профессиональными средами в компьютерном дизайне, программировании и системном администрировании.\n\n" +
                                                                                                 "На этой ступени дети знакомятся с векторной и растровой графикой в программах СorelDraw и Photoshop, изучают робототехнику на конструкторах, Arduino, верстают сайты на HTML.\n" +
                                                                                                 "«Инфостарт» поможет тем, кто только что познакомился с Инфосферой и хочет продвинуться в области IT.\n");

                stream.Close();
            }
        }

        private async Task HandleAdminQueryAsync(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            long instigatorId = callbackQuery.Message.Chat.Id;

            ConsoleStatics.CreateLog($"Received new admin query!\nName: {callbackQuery.Message.Chat.FirstName} | ID: {instigatorId} | Callback: {callbackQuery.Data}");

            if (callbackQuery.Data == "!ban")
            {
                await TrySendTextMessageAsync(instigatorId, "Напишите ID пользователя, которого хотите забанить.", replyMarkup: new ForceReplyMarkup() { Selective = true });
            }
            else if (callbackQuery.Data == "!unban")
            {
                await TrySendTextMessageAsync(instigatorId, "Напишите ID пользователя, которого хотите разбанить.", replyMarkup: new ForceReplyMarkup() { Selective = true });
            }
            else if (callbackQuery.Data == "!make-an-admin")
            {
                await TrySendTextMessageAsync(instigatorId, "Напишите ID пользователя, которого хотите сделать администратором.", replyMarkup: new ForceReplyMarkup() { Selective = true });
            }
            else if (callbackQuery.Data == "!make-a-user")
            {
                await TrySendTextMessageAsync(instigatorId, "Напишите ID пользователя, которого хотите лишить привелегий администратора.", replyMarkup: new ForceReplyMarkup() { Selective = true });
            }
        }

        private void StartClearingLastUpdates(int delayInSeconds)
        {
            new Task(() =>
            {
                while (true)
                {
                    Thread.Sleep(delayInSeconds * 1000);

                    _lastUpdates.Clear();
                }
            }).Start();
        }

        private void StartClearingLastSpammers(int delayInSeconds)
        {
            new Task(() =>
            {
                while (true)
                {
                    Thread.Sleep(delayInSeconds * 1000);

                    _spammers.Clear();
                }
            }).Start();
        }

        private bool IsSpam(Chat user)
        {
            long instigatorId = user.Id;

            if (_lastUpdates.Contains(instigatorId))
            {
                if (!_spammers.TryAdd(instigatorId, 1))
                {
                    _spammers[instigatorId]++;

                    if (_spammers[instigatorId] > 5)
                    {
                        _spammers.Remove(instigatorId);

                        //SpamRestrict(user);

                        return true;
                    }

                    return true;
                }
            }

            _lastUpdates.Add(instigatorId);

            return false;
        }

        //private void SpamRestrict(Chat user)
        //{
        //    long instigatorId = user.Id;

        //    ConsoleStatics.CreateLog("Spam restricted...", ConsoleColor.Yellow);

        //    if (LongPollStatics.BannedUsers.Add(new User(instigatorId, user.FirstName, user.LastName, user.Username)))
        //    {
        //        ConsoleStatics.CreateLog($"Spammer has been banned!\nID: {instigatorId}", ConsoleColor.Blue);

        //        _ = TrySendTextMessageAsync(user.Id, "Извините, но мы вынуждены игнорировать ваши сообщения.");
        //    }
        //}

        private async Task TrySendTextMessageAsync(ChatId chatId, string text, IReplyMarkup? replyMarkup = null)
        {
            Task<Message> resultTask = Client.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup);

            try
            {
                await resultTask;
            }
            catch (Exception ex)
            {
                ConsoleStatics.CreateLog(ex.ToString(), ConsoleColor.Red);
            }
        }

        private async Task TrySendPhotoAsync(ChatId chatId, InputFile photo, string? caption = null, IReplyMarkup? replyMarkup = null)
        {
            Task<Message> resultTask = Client.SendPhotoAsync(chatId, photo, caption: caption, replyMarkup: replyMarkup);

            try
            {
                await resultTask;
            }
            catch (Exception ex)
            {
                ConsoleStatics.CreateLog(ex.ToString(), ConsoleColor.Red);
            }
        }
    }
}
