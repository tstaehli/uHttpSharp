using System.IO;
using System.Net;
using System.Net.Sockets;

namespace uhttpsharp.Clients
{
    public class TcpClientAdapter : IClient
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        public TcpClientAdapter(TcpClient client)
        {
            _client = client;
            _stream = _client.GetStream();

            // Read Timeout of one second.
            _stream.ReadTimeout = 1000;
        }

        public Stream Stream
        {
            get { return _stream; }
        }

        public bool Connected
        {
            get { return _client.Connected; }
        }

        public void Close()
        {
            _client.Close();
        }


        public EndPoint RemoteEndPoint
        {
            get { return _client.Client.RemoteEndPoint; }
        }
    }
}