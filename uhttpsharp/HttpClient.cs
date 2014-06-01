/*
 * Copyright (C) 2011 uhttpsharp project - http://github.com/raistlinthewiz/uhttpsharp
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.

 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.

 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Web.Caching;
using log4net;
using System.Net;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using uhttpsharp.Clients;
using uhttpsharp.Headers;
using uhttpsharp.RequestProviders;

namespace uhttpsharp
{
    internal sealed class HttpClientHandler
    {
        private const string CrLf = "\r\n";
        private static readonly byte[] CrLfBuffer = Encoding.UTF8.GetBytes(CrLf);

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IAsyncBlockingQueue<IHttpContext> _requests = new AsyncBlockingQueue<IHttpContext>(10);
        private readonly IAsyncBlockingQueue<IHttpContext> _responses = new AsyncBlockingQueue<IHttpContext>(10);

        private readonly IClient _client;
        private readonly Func<IHttpContext, Task> _requestHandler;
        private readonly IHttpRequestProvider _requestProvider;
        private readonly EndPoint _remoteEndPoint;
        private DateTime _lastOperationTime;
        private readonly Stream _stream;

        public HttpClientHandler(IClient client, Func<IHttpContext, Task> requestHandler, IHttpRequestProvider requestProvider)
        {
            _remoteEndPoint = client.RemoteEndPoint;
            _client = client;
            _requestHandler = requestHandler;
            _requestProvider = requestProvider;

            _stream = new BufferedStream(_client.Stream);
            Logger.InfoFormat("Got Client {0}", _remoteEndPoint);

            Task.Factory.StartNew(ReadRequests);
            Task.Factory.StartNew(ProcessRequests);
            Task.Factory.StartNew(SendResponses);


            UpdateLastOperationTime();
        }

        private async void SendResponses()
        {
            try
            {
                while (Client.Connected)
                {
                    var item = await _responses.Dequeue().ConfigureAwait(false);

                    if (item.Response == null)
                    {
                        Client.Close();
                        return;
                    }

                    var streamWriter = new StreamWriter(_stream);
                    await WriteResponse(item, streamWriter).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                // Hate people who make bad calls.
                Logger.Warn(string.Format("Error while serving : {0}", _remoteEndPoint), e);

                _client.Close();
            }

        }
        private async void ProcessRequests()
        {
            try
            {
                while (Client.Connected)
                {
                    var item = await _requests.Dequeue().ConfigureAwait(false);
                    await _requestHandler(item).ConfigureAwait(false);
                    await _responses.Enqueue(item).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                // Hate people who make bad calls.
                Logger.Warn(string.Format("Error while serving : {0}", _remoteEndPoint), e);

                _client.Close();
            }
        }

        private async void ReadRequests()
        {
            try
            {
                var streamReader = new StreamReader(_stream);

                while (_client.Connected)
                {
                    var request = await _requestProvider.Provide(streamReader).ConfigureAwait(false);

                    if (request != null)
                    {
                        UpdateLastOperationTime();

                        var context = new HttpContext(request, _client.RemoteEndPoint);

                        Logger.InfoFormat("{1} : Got request {0}", request.Uri, _client.RemoteEndPoint);

                        await _requests.Enqueue(context).ConfigureAwait(false);
                    }
                    else
                    {
                        _client.Close();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                // Hate people who make bad calls.
                Logger.Warn(string.Format("Error while serving : {0}", _remoteEndPoint), e);
                _client.Close();
            }

            Logger.InfoFormat("Lost Client {0}", _remoteEndPoint);
        }
        private async Task WriteResponse(IHttpContext context, StreamWriter writer)
        {
            IHttpResponse response = context.Response;
            IHttpRequest request = context.Request;

            // Headers
            await response.WriteHeaders(writer).ConfigureAwait(false);

            // Cookies
            if (context.Cookies.Touched)
            {
                await writer.WriteAsync(context.Cookies.ToCookieData())
                    .ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }

            // Empty Line
            await writer.BaseStream.WriteAsync(CrLfBuffer, 0, CrLfBuffer.Length).ConfigureAwait(false);

            // Body
            await response.WriteResponse(writer).ConfigureAwait(false);

            if (!request.Headers.KeepAliveConnection() || response.CloseConnection)
            {
                _client.Close();
            }
        }

        public IClient Client
        {
            get { return _client; }
        }

        public void ForceClose()
        {
            _client.Close();
        }

        public DateTime LastOperationTime
        {
            get
            {
                return _lastOperationTime;
            }
        }

        private void UpdateLastOperationTime()
        {
            // _lastOperationTime = DateTime.Now;
        }

    }


    public static class RequestHandlersAggregateExtensions
    {

        public static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers)
        {
            return handlers.Aggregate(0);
        }

        private static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers, int index)
        {
            if (index == handlers.Count)
            {
                return null;
            }

            var currentHandler = handlers[index];
            var nextHandler = handlers.Aggregate(index + 1);

            return context => currentHandler.Handle(context, () => nextHandler(context));
        }


    }
}