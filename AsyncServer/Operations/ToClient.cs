using AsyncServer.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace AsyncServer.Operations
{
    internal static class ToClient
    {
        internal static void CommandList( Socket target)
        {
            var cmds = Commands.List();
            string res = "Commands : ";
            foreach( var cmd in cmds )
            {
                res += cmd.Caller + " ";
            }
            Console.WriteLine( res );
            byte[] data = Encoding.ASCII.GetBytes( res );
            target.Send( data );
        }

        internal static void CommandHelp( Socket target, string caller )
        {
            var cmds = Commands.List();
            var res = "";
            foreach( var cmd in cmds )
            {
                if( cmd.Caller == caller )
                    res = cmd.ToString();
            }
            byte[] data = Encoding.ASCII.GetBytes( res );
            target.Send( data );
        }
    }
}
