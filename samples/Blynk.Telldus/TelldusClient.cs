using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Telldus
{
    public class TelldusClient : IDisposable
    {
        private Websocket socket;
        public TelldusClient(string uri)
        {
            this.socket = new Websocket(uri);
            this.socket.OnMessage += m =>
            {
                var @object = JObject.Parse(m);
                var data = @object["data"].ToObject<string>();
                if (data != null)
                {
                    try
                    {
                        var json = JObject.Parse(data);
                        this.OnMessage?.Invoke(json);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(data + " " + e.Message);
                    }


                }
            };
        }
        public async Task ConnectAsync()
        {
            await this.socket.ConnectAsync();
        }

        public void StartMessageLoop()
        {
            this.socket.StartMessageLoop();
        }

        public Action<JObject> OnMessage { get; set; }

        public async Task CloseAsync()
        {
            await this.socket.CloseAsync();
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
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~TelldusClient()
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
