using AsyncServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// State object for reading client data asynchronously  
public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

namespace AsyncServer
{
    public class AsynchronousSocketListener
    {


        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<User> users = new List<User>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 420;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine( "Setting up server..." );
            serverSocket.Bind( new IPEndPoint( IPAddress.Any, PORT ) );
            serverSocket.Listen( 0 );
            serverSocket.BeginAccept( AcceptCallback, null );
            Console.WriteLine( "Server setup complete" );
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach( User user in users )
            {
                user.Socket.Shutdown( SocketShutdown.Both );
                user.Socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback( IAsyncResult AR )
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept( AR );
            }
            catch( ObjectDisposedException ) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            users.Add( new User( socket ) );
            socket.BeginReceive( buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket );
            Console.WriteLine( "Client connected, waiting for request..." );
            serverSocket.BeginAccept( AcceptCallback, null );
        }

        private static void ReceiveCallback( IAsyncResult AR )
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            User currUser = users.Where(u => u.Socket == current).FirstOrDefault();

            try
            {
                received = currUser.Socket.EndReceive( AR );
            }
            catch( SocketException )
            {
                Console.WriteLine( "Client forcefully disconnected" );
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currUser.Socket.Close();
                users.Remove( currUser );
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy( buffer, recBuf, received );
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine( "Received Text: " + text );
            if( text.Substring( 0, 1 ) == "$" ) // potential request
            {
                if( text.ToLower() == "$gettime" ) // Client requested time
                {
                    Console.WriteLine( "Text is a get time request" );
                    byte[] data = Encoding.ASCII.GetBytes(DateTime.Now.ToLongTimeString());
                    current.Send( data );
                    Console.WriteLine( "Time sent to client" );
                }
                else if( text.ToLower() == "$exit" ) // Client wants to exit gracefully
                {
                    currUser.Socket.Shutdown( SocketShutdown.Both );
                    currUser.Socket.Close();
                    users.Remove( currUser );
                    Console.WriteLine( "Client disconnected" );
                    return;
                }
                else if( text.ToLower().StartsWith("$nick " ) )
                {
                    string nick = text.ToLower().Substring( 6 );
                    currUser.SetNickName( nick );
                    Console.WriteLine( String.Format("new nickname set => {0}", currUser.Nickname ) );
                    byte[] data = Encoding.ASCII.GetBytes(String.Format("new nickname set => {0}", currUser.Nickname ));
                    currUser.Socket.Send( data );
                }
                else if( text.ToLower() == "$user" )
                {
                    string res = String.Format("--JSON{0}", JsonConvert.SerializeObject(currUser.Nickname));
                    byte[] data = Encoding.ASCII.GetBytes(res);
                    currUser.Socket.Send( data );
                }
                else if( text.ToLower() == "$rdm" )
                {
                    Operations.Async.TestApi( currUser.Socket );
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
                foreach( User user in users )
                {
                    user.Socket.Send( data );
                }
                Console.WriteLine( "Message broadcasted" );
            }

            current.BeginReceive( buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current );
        }

    }
}
