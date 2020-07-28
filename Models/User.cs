using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace AsyncServer.Models
{
    internal class User
    {
        public Guid Id { get; private set; }
        public string Nickname { get; private set; }
        public Socket Socket { get; private set; }

        public User(Socket socket)
        {
            Socket = socket;
        }

        public Guid SetGuid()
        {
            Id = new Guid();
            return Id;
        }

        public String SetNickName( string nickname )
        {
            if( nickname.Length > 20 )
                Nickname = nickname.Substring( 0, 20 );
            else
                Nickname = nickname;
            return Nickname;
        }
    }
}
