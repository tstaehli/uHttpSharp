using System.IO;
using System.Net;
using System.Net.Sockets;

namespace uhttpsharp.Clients
{
    public class TcpClientAdapter : IClient
    {
        private readonly TcpClient _client;
        private bool _forcedDisconnection;

        public TcpClientAdapter(TcpClient client)
        {
            _client = client;
        }

        public Stream Stream
        {
            get { return _client.GetStream(); }
        }

        public bool Connected
        {
            get { return _client.Connected && !_forcedDisconnection; }
        }

        public void Close()
        {
            _client.Close();
            _forcedDisconnection = true;
        }


        public EndPoint RemoteEndPoint
        {
            get { return _client.Client.RemoteEndPoint; }
        }
    }
}