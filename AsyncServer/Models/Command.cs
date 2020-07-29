using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncServer.Models
{
    public class Command
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Caller { get; set; }
        public string Description { get; set; }
        public int AuthLevel { get; set; }

        public Command(){}

        public Command(int id, string name, string caller, string desc = "", int authLevel = 9)
        {
            Id = id;
            Name = name;
            Caller = caller;
            Description = desc;
            AuthLevel = authLevel;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1}: {2} (auth:{3})", Caller, Name, Description, AuthLevel );
        }
    }
}
