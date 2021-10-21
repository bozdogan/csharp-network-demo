using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
            EchoServer server = new EchoServer();
            server.Start();

            Console.ReadLine();
        }
    }
}
