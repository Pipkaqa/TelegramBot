#pragma warning disable CS8604, CS8600, CS8602

using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfosphereBot
{
    public static class ConsoleStatics
    {
        public static readonly ConsoleColor DefaultConsoleColor = ConsoleColor.Green;

        public static readonly string CommandError = "Unknown command.";

        public static readonly string TooManyArgumentsError = "Too many arguments for this command.";

        public static readonly string TooFewArgumentsError = "Too few arguments for this command.";

        public static readonly string WrongArgumentsError = "Wrong arguments for this command.";

        public static void CreateLog(string message, ConsoleColor color = ConsoleColor.Green, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message, args);
            Console.ForegroundColor = DefaultConsoleColor;

            string logType;

            if (color == DefaultConsoleColor)
            {
                logType = "log";
                Console.WriteLine();

            }
            else if (color == ConsoleColor.Yellow)
            {
                logType = "action";
                Console.WriteLine();

            }
            else if (color == ConsoleColor.Blue)
            {
                logType = "info";
                Console.WriteLine();

            }
            else if (color == ConsoleColor.Red)
            {
                logType = "error";
                Console.WriteLine();

            }
            else if (color == ConsoleColor.Magenta)
            {
                logType = "exit";
            }
            else
            {
                logType = "unknown";
                Console.WriteLine();
            }

            try
            {
                StreamWriter streamWriter = new(BotStatics.GetTodayLogFilePath(), append: true);

                string[] lines = message.Split('\n');

                string time = $"{DateTime.Now.Hour + 3:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00}";

                foreach (string line in lines)
                {
                    streamWriter.WriteLine($"{logType}: {time} {line}");
                }

                streamWriter.Close();
            }
            catch (Exception exception)
            {
                CreateLog($"Fatal Error:\n{exception.Message}", ConsoleColor.Red);
            }
        }

        public static void HandleConsoleCommands(ITelegramBotClient client, JsonDatabase admins, JsonDatabase bannedUsers)
        {
            bool error;
            string command;

            Console.ForegroundColor = DefaultConsoleColor;

            CreateLog("Available console commands:\n" +
                      "!ban, arguments: id;\n" +
                      "!unban, arguments: id;\n" +
                      "!make-an-admin, arguments: id;\n" +
                      "!make-a-user, arguments: id;\n" +
                      "!help, arguments: none", ConsoleColor.Blue);

            while (true)
            {
                error = false;
                command = Console.ReadLine();

                Console.WriteLine();

                CreateLog($"Processed console command - {command}", ConsoleColor.Yellow);

                string[] data = command.Split(' ');

                if (data[0] == "!ban")
                {
                    if (data.Length <= 1 || data[1] == null || data[1] == "")
                    {
                        CreateLog(TooFewArgumentsError, ConsoleColor.Red);
                        continue;
                    }

                    foreach (char c in data[1])
                    {
                        if (!char.IsDigit(c))
                        {
                            CreateLog(WrongArgumentsError, ConsoleColor.Red);
                            error = true;
                            break;
                        }
                    }

                    if (error)
                    {
                        continue;
                    }

                    long targetId = Convert.ToInt64(data[1]);

                    Task<Chat> getChatTask = client.GetChatAsync(targetId);

                    try
                    {
                        getChatTask.Wait();

                        Chat user = getChatTask.Result;

                        if (!bannedUsers.Add(new User(targetId, user.FirstName, user.LastName, user.Username)))
                        {
                            CreateLog($"User already is banned!\nID: {targetId}", ConsoleColor.Blue);
                            continue;
                        }

                        CreateLog($"User has been banned!\nID: {targetId}", ConsoleColor.Blue);
                    }
                    catch
                    {
                        CreateLog($"User doesn't exist!\nID: {targetId}", ConsoleColor.Blue);
                    }
                }
                else if (data[0] == "!unban")
                {
                    if (data.Length <= 1 || data[1] == null || data[1] == "")
                    {
                        CreateLog(TooFewArgumentsError, ConsoleColor.Red);
                        continue;
                    }

                    foreach (char c in data[1])
                    {
                        if (!char.IsDigit(c))
                        {
                            CreateLog(WrongArgumentsError, ConsoleColor.Red);
                            error = true;
                            break;
                        }
                    }

                    if (error)
                    {
                        continue;
                    }

                    long targetId = Convert.ToInt64(data[1]);

                    if (!bannedUsers.Remove(targetId))
                    {
                        CreateLog($"User is not banned!\nID: {targetId}", ConsoleColor.Blue);
                        continue;
                    }

                    CreateLog($"User has been unbanned!\nID: {targetId}", ConsoleColor.Blue);
                }
                else if (data[0] == "!make-an-admin")
                {
                    if (data.Length <= 1 || data[1] == null || data[1] == "")
                    {
                        CreateLog(TooFewArgumentsError, ConsoleColor.Red);
                        continue;
                    }

                    foreach (char c in data[1])
                    {
                        if (!char.IsDigit(c))
                        {
                            CreateLog(WrongArgumentsError, ConsoleColor.Red);
                            error = true;
                            break;
                        }
                    }

                    if (error)
                    {
                        continue;
                    }

                    long targetId = Convert.ToInt64(data[1]);

                    Task<Chat> task = client.GetChatAsync(targetId);

                    try
                    {
                        task.Wait();

                        Chat user = task.Result;

                        if (!admins.Add(new User(targetId, user.FirstName, user.LastName, user.Username)))
                        {
                            CreateLog($"User already is admin!\nID: {targetId}", ConsoleColor.Blue);
                            continue;
                        }

                        CreateLog($"User has been promoted to admin!\nID: {targetId}", ConsoleColor.Blue);
                    }
                    catch
                    {
                        CreateLog($"User doesn't exist!\nID: {targetId}", ConsoleColor.Blue);
                    }
                }
                else if (data[0] == "!make-a-user")
                {
                    if (data.Length <= 1 || data[1] == null || data[1] == "")
                    {
                        CreateLog(TooFewArgumentsError, ConsoleColor.Red);
                        continue;
                    }

                    foreach (char c in data[1])
                    {
                        if (!char.IsDigit(c))
                        {
                            CreateLog(WrongArgumentsError, ConsoleColor.Red);
                            error = true;
                            break;
                        }
                    }

                    if (error)
                    {
                        continue;
                    }

                    long targetId = Convert.ToInt64(data[1]);

                    if (!admins.Remove(targetId))
                    {
                        CreateLog($"User is not an admin!\nID: {targetId}", ConsoleColor.Blue);
                        continue;
                    }

                    CreateLog($"User has been demoted from admin!\nID: {targetId}", ConsoleColor.Blue);
                }
                else if (data[0] == "!help")
                {
                    if (data.Length > 1)
                    {
                        CreateLog(TooManyArgumentsError, ConsoleColor.Red);
                        continue;
                    }

                    CreateLog("Available console commands:\n" +
                              "!ban, arguments: id;\n" +
                              "!unban, arguments: id;\n" +
                              "!make-an-admin, arguments: id;\n" +
                              "!make-a-user, arguments: id;\n" +
                              "!help, arguments: none", ConsoleColor.Blue);
                }
                else
                {
                    CreateLog(CommandError, ConsoleColor.Red);
                }
            }
        }
    }
}
