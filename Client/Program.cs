using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client
{
    public class Message
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }

        public override string ToString()
        {
            return $"Message(IntProperty={this.IntProperty}, StringProperty=\"{this.StringProperty}\")";
        }
    }

    class Program
    {
        static async Task SendAsync<T>(NetworkStream stream, T message)
        {
            var (header, body) = Encode(message);
            await stream.WriteAsync(header, 0, header.Length);
            await stream.WriteAsync(body, 0, body.Length);
        }

        static async Task<T> ReceiveAsync<T>(NetworkStream stream)
        {
            byte[] header = new byte[4];
            header = await ReadAsync(stream, 4);
            int bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header));

            byte[] body = await ReadAsync(stream, bodyLength);
            return Decode<T>(body);
        }

        static (byte[] header, byte[] body) Encode<T>(T message)
        {
            // NOTE(bora): XmlSerializer can serialize objects for us so we
            // don't need to implement custom methods for all our classes.
            XmlSerializer x = new XmlSerializer(typeof(T));
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter writer = new StringWriter(stringBuilder);
            x.Serialize(writer, message);

            byte[] body = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));  // NOTE(bora): This will return a `byte[4]`

            return (header, body);
        }

        static T Decode<T>(byte[] body)
        {
            string bodyStr = Encoding.UTF8.GetString(body);
            XmlSerializer x = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(bodyStr);

            return (T)x.Deserialize(reader);
        }

        static async Task<byte[]> ReadAsync(NetworkStream stream, int numBytesToRead)
        {
            byte[] buffer = new byte[numBytesToRead];
            int numBytesRead = 0;

            while(numBytesRead < numBytesToRead)
            {
                int numRemainingBytes = (numBytesToRead - numBytesRead);
                int numReceivedBytes = await stream.ReadAsync(buffer, numBytesRead, numRemainingBytes).ConfigureAwait(false);

                if(numReceivedBytes != 0)
                {
                    numBytesRead += numReceivedBytes;
                }
                else
                {
                    throw new Exception("Connection Lost");
                }
            }

            return buffer;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Press Enter to Connect");
            Console.ReadLine();

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000);
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(endPoint);
            NetworkStream networkstream = new NetworkStream(socket, true);

            Message message = new Message {
                IntProperty = 200,
                StringProperty = "Hello World"
            };

            Console.Write("Sending ");
            Console.WriteLine(message);

            await SendAsync(networkstream, message).ConfigureAwait(false);

            Message response = await ReceiveAsync<Message>(networkstream);
            Console.WriteLine($"Received {response}");

            Console.ReadLine();
        }
    }
}
