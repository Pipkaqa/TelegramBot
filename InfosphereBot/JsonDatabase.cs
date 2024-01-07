#pragma warning disable CS8604, CS8600

using Newtonsoft.Json;

namespace InfosphereBot
{
    public class JsonDatabase
    {
        public JsonDatabase(string fullPath)
        {
            _databasePath = fullPath;

            if (!File.Exists(_databasePath))
            {
                File.Create(_databasePath).Close();
            }
        }

        private readonly string _databasePath;

        public bool Add(User user)
        {
            List<User> users = ReadAll();

            if (Contains(user.Id))
            {
                return false;
            }

            users.Add(user);

            string json = JsonConvert.SerializeObject(users);

            File.WriteAllText(_databasePath, json);

            return true;
        }

        public async Task<bool> AddAsync(User user)
        {
            List<User> users = await ReadAllAsync();

            if (await ContainsAsync(user.Id))
            {
                return false;
            }

            users.Add(user);

            string json = JsonConvert.SerializeObject(users);

            await File.WriteAllTextAsync(_databasePath, json);

            return true;
        }

        public bool Remove(long id)
        {
            List<User> users = ReadAll();

            User toDelete = users.FirstOrDefault(user => user.Id == id);

            if (toDelete == null)
            {
                return false;
            }

            users.Remove(toDelete);

            string json = JsonConvert.SerializeObject(users);

            File.WriteAllText(_databasePath, json);

            return true;
        }

        public async Task<bool> RemoveAsync(long id)
        {
            List<User> users = await ReadAllAsync();

            User toDelete = users.FirstOrDefault(user => user.Id == id);

            if (toDelete == null)
            {
                return false;
            }

            users.Remove(toDelete);

            string json = JsonConvert.SerializeObject(users);

            await File.WriteAllTextAsync(_databasePath, json);

            return true;
        }

        public bool Contains(long id)
        {
            List<User> users = ReadAll();

            User user = users.FirstOrDefault(user => user.Id == id);

            if (user == null)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ContainsAsync(long id)
        {
            List<User> users = await ReadAllAsync();

            User user = users.FirstOrDefault(user => user.Id == id);

            if (user == null)
            {
                return false;
            }

            return true;
        }

        public List<User> ReadAll()
        {
            string json = File.ReadAllText(_databasePath);

            List<User> users = JsonConvert.DeserializeObject<List<User>>(json);

            if (users == null)
            {
                return new List<User>();
            }

            return users;
        }

        public async Task<List<User>> ReadAllAsync()
        {
            string json = await File.ReadAllTextAsync(_databasePath);

            List<User> users = JsonConvert.DeserializeObject<List<User>>(json);

            if (users == null)
            {
                return new List<User>();
            }

            return users;
        }
    }
}
