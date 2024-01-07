namespace InfosphereBot
{
    public static class BotStatics
    {
        static BotStatics()
        {
            if (!Directory.Exists(JsonFolderPath))
            {
                Directory.CreateDirectory(JsonFolderPath);
            }

            if (!Directory.Exists(PicturesFolderPath))
            {
                Directory.CreateDirectory(PicturesFolderPath);
            }

            if (!Directory.Exists(LogsFolderPath))
            {
                Directory.CreateDirectory(LogsFolderPath);
            }

            string todayLogFilePath = GetTodayLogFilePath();

            if (!System.IO.File.Exists(todayLogFilePath))
            {
                System.IO.File.Create(todayLogFilePath).Close();
            }

            Admins = new(Path.Combine(JsonFolderPath, "Admins.json"));
            BannedUsers = new(Path.Combine(JsonFolderPath, "BannedUsers.json"));
        }

        public static readonly string PicturesFolderPath = Path.Combine(Environment.CurrentDirectory, "Pictures");
        public static readonly string JsonFolderPath = Path.Combine(Environment.CurrentDirectory, "Json");
        public static readonly string LogsFolderPath = Path.Combine(Environment.CurrentDirectory, "Logs");

        public static readonly JsonDatabase Admins;
        public static readonly JsonDatabase BannedUsers;

        public static readonly string Url = "https://nn.isphera.ru/#form";
        public static readonly string UnknownCommandMessage = "Извините, но я не знаю такой команды...";

        public static readonly int UpdatesClearingDelayInSeconds = 1;
        public static readonly int SpammersClearingDelayInSeconds = 30;

        public static async Task<bool> IsBannedAsync(long instigatorId, string firstName = "Unknown", string text = "None")
        {
            if (await BannedUsers.ContainsAsync(instigatorId))
            {
                ConsoleStatics.CreateLog($"Banned user tries to speak!\nName: {firstName} | ID: {instigatorId} | Text: {text}");
                return true;
            }

            return false;
        }

        public static bool IsBanned(long instigatorId, string firstName = "Unknown", string text = "None")
        {
            if (BannedUsers.Contains(instigatorId))
            {
                ConsoleStatics.CreateLog($"Banned user tries to speak!\nName: {firstName} | ID: {instigatorId} | Text: {text}");
                return true;
            }

            return false;
        }

        public static string GetTodayLogFilePath() => Path.Combine(LogsFolderPath, "Session_" + DateTime.Today.Day + "_" + DateTime.Today.Month + "_" + DateTime.Today.Year + ".txt");

        public static void OnExit(object sender, EventArgs e)
        {
            ConsoleStatics.CreateLog("Disabling the bot...", ConsoleColor.Magenta);
        }
    }
}
