using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Telldus
{
    public class Websocket : IDisposable
    {
        private ClientWebSocket socket;
        private Uri uri;
        private Task messageLoop;
        public Action<string> OnMessage { get; set; }

        public Websocket(string uri)
        {
            this.uri = new Uri(uri);
            this.socket = new ClientWebSocket();
        }
        public async Task ConnectAsync()
        {
            await this.socket.ConnectAsync(this.uri, CancellationToken.None);
        }
        
        public void StartMessageLoop()
        {
            this.messageLoop = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult wsresult = null;
                var totalString = string.Empty;
                try
                {
                    do
                    {
                        wsresult = await this.socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (wsresult.MessageType == WebSocketMessageType.Text)
                        {
                            var part = System.Text.Encoding.UTF8.GetString(buffer, 0, wsresult.Count);
                            totalString += part;
                            if (wsresult.EndOfMessage)
                            {
                                this.OnMessage?.Invoke(totalString);
                                totalString = string.Empty;
                            }
                        }

                    } while (wsresult != null && !wsresult.CloseStatus.HasValue);

                    await this.socket.CloseAsync(wsresult.CloseStatus.Value, wsresult.CloseStatusDescription, CancellationToken.None);
                }
                catch
                {

                }
            });
        }

        public async Task CloseAsync()
        {
            await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", default(CancellationToken));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                this.socket?.Dispose();
                this.socket = null;
                this.messageLoop?.Wait();
                this.messageLoop?.Dispose();
                this.messageLoop = null;
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Websocket()
        {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
