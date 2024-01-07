namespace InfosphereBot
{
    public class User
    {
        public User(long id, string firstName, string lastName, string userName)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            UserName = userName;
        }

        public long Id;
        public string FirstName;
        public string LastName;
        public string UserName;
    }
}
