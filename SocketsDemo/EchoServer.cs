using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    class EchoServer
    {
        public void Start(int port=3000)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);
            socket.Listen(128);

            Console.WriteLine("SERVER ONLINE");
            Task.Run(() => DoEcho(socket));  // NOTE(bora): This starts the loop.
        }

        private async Task DoEcho(Socket socket)
        {
            /* NOTE(bora):
                .NET's Socket API is asynchonous by default so we need this wrapper
                code to run it in a synchonous execution.
            */

            while(true)
            {
                Socket clientSocket = await Task.Factory.FromAsync(
                    new Func<AsyncCallback, object, IAsyncResult>(socket.BeginAccept),
                    new Func<IAsyncResult, Socket>(socket.EndAccept),
                    null).ConfigureAwait(false);

                Console.WriteLine("[DoEcho]: Client is connected");

                NetworkStream stream = new NetworkStream(clientSocket);
                byte[] buffer = new byte[1024];
                while(true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if(bytesRead == 0)
                    {
                        // NOTE(bora): If client didn't end the connection normally, reading nothing
                        // probably means connection lost between client and server.
                        Console.WriteLine("[DoEcho]: Client is disconnected");
                        break;
                    }

                    await stream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }
            }
        }
    }
}
