
using AsyncServer.Models;
using System.Collections.Generic;

namespace AsyncServer.Utils
{
    internal static class Enums
    {

    }

    public static class Commands
    {
        public static List<Command> List()
        {
            var res = new List<Command>();

            res.Add( new Command( 1, "Exit", "$exit", "Close your connection.", 0 ) );
            res.Add( new Command( 3, "Nickname", "$nick", "Set a new nickname.", 0 ) );
            res.Add( new Command( 2, "Help", "$help", "Return the list of available commands.", 0 ) );
            res.Add( new Command( 4, "User info", "$user", "Get your user profile.", 0 ) );
            res.Add( new Command( 5, "RandomPeople", "$rdm", "Return a random generated person. API test.", 0 ) );

            return res;
        }


        public const string EXIT = "$exit";
        public const string NICKNAME = "$nick";
        public const string USER = "$user";
        public const string RANDOM = "$rdm";
        public const string HELP_COMMAND = "$help";
    }
}
