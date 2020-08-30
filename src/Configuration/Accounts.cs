namespace ClassicUO.Configuration
{
    internal class Account 
    {
        public Account()
        {
        }

        public Account(string server, string userName, string password)
        {
            Server = server;
            UserName = userName;
            Password = password;

        }

        public void UpdatePassword(string newPassword)
        {
            Password = newPassword;
        }

        public string Server { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
