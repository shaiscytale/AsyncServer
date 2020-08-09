using AsyncServer.Models;
using AsyncServer.Utils;
using Datalink.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace AsyncServer
{
    public class AsynchronousSocketListener
    {
        private static readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<User> _users = new List<User>();
        private const int BUFFER_SIZE = 2048;
        private const int DEFAULT_PORT = 420;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine( "Setting up server..." );
            _serverSocket.Bind( new IPEndPoint( IPAddress.Any, DEFAULT_PORT ) );
            _serverSocket.Listen( 0 );
            _serverSocket.BeginAccept( AcceptCallback, null );
            Console.WriteLine( "Server setup complete" );
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach( User user in _users )
            {
                user.Socket.Shutdown( SocketShutdown.Both );
                user.Socket.Close();
            }

            _serverSocket.Close();
        }

        private static void AcceptCallback( IAsyncResult AR )
        {
            Socket socket;

            try
            {
                socket = _serverSocket.EndAccept( AR );
            }
            catch( ObjectDisposedException )
            {
                return;
            }

            _users.Add( new User( socket ) );
            socket.BeginReceive( buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket );
            Console.WriteLine( "New client connected, waiting for his request..." );
            _serverSocket.BeginAccept( AcceptCallback, null );
        }

        private static void ReceiveCallback( IAsyncResult AR )
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            User currUser = _users.Where(u => u.Socket == current).FirstOrDefault();

            try
            {
                received = currUser.Socket.EndReceive( AR );
            }
            catch( SocketException )
            {
                Console.WriteLine( "Client hard disconnected" );
                currUser.Socket.Close();
                _users.Remove( currUser );
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy( buffer, recBuf, received );

            // try to deserialize the received buffer into a Bag
            Bag _bag;
            try
            {
                _bag = new Bag( recBuf );
            }
            catch( Exception )
            {
                Console.WriteLine("error during bag-deserialization");
                return;
            }

            if(_bag != null )
            {
                var cmd = Command.GetById(_bag.CommandId);
                byte[] data;
                if(cmd == null )
                     data = Encoding.ASCII.GetBytes(String.Format("{0} asked for an unknown command (id:{1})", currUser.Nickname, _bag.CommandId ) );
                else
                    data = Encoding.ASCII.GetBytes(String.Format("{0} asked for {1}", currUser.Nickname, cmd.Description ));
                currUser.Socket.Send( data );
            }
            else
            {
                string text = Encoding.ASCII.GetString(recBuf);
                Console.WriteLine( "Received Text: " + text );
                if( text.Substring( 0, 1 ) == "$" ) // potential request
                {
                    if( text.ToLower() == Command.EXIT ) // Client wants to exit gracefully
                    {
                        currUser.Socket.Shutdown( SocketShutdown.Both );
                        currUser.Socket.Close();
                        _users.Remove( currUser );
                        Console.WriteLine( "Client disconnected" );
                        return;
                    }
                    else if( text.ToLower() == Command.NICKNAME )
                    {
                        string nick = text.ToLower().Substring( 6 );
                        currUser.SetNickName( nick );
                        Console.WriteLine( String.Format( "new nickname set => {0}", currUser.Nickname ) );
                        byte[] data = Encoding.ASCII.GetBytes(String.Format("new nickname set => {0}", currUser.Nickname ));
                        currUser.Socket.Send( data );
                    }
                    else if( text.ToLower() == Command.USER )
                    {
                        string res = String.Format("--JSON{0}", JsonConvert.SerializeObject(currUser.Nickname));
                        byte[] data = Encoding.ASCII.GetBytes(res);
                        currUser.Socket.Send( data );
                    }
                    else if( text.ToLower() == Command.RANDOM )
                    {
                        Operations.Async.TestApi( currUser.Socket );
                    }
                    else if( text.ToLower() == Command.HELP_COMMAND )
                    {
                        Operations.ToClient.CommandList( currUser.Socket );
                    }
                    else if( text.ToLower().StartsWith( Command.HELP_COMMAND + " -" ) )
                    {
                        var len = (Command.HELP_COMMAND+" -").Length;

                        var caller = text.Substring(len);

                        Operations.ToClient.CommandHelp( currUser.Socket, caller );
                    }
                    else
                    {
                        Console.WriteLine( "Text is an invalid request" );
                        byte[] data = Encoding.ASCII.GetBytes("Invalid request");
                        currUser.Socket.Send( data );
                        Console.WriteLine( "Warning Sent" );
                    }
                }
                else // normal messages
                {
                    Console.WriteLine( "Text is a normal message" );
                    string sender = currUser.Nickname ?? ((IPEndPoint)currUser.Socket.RemoteEndPoint ).Address.ToString();
                    byte[] data = Encoding.ASCII.GetBytes(String.Format("{0} said => {1}", sender, text));
                    foreach( User user in _users )
                    {
                        user.Socket.Send( data );
                    }
                    Console.WriteLine( "Message broadcasted" );
                }
            }



            current.BeginReceive( buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current );
        }

    }
}
