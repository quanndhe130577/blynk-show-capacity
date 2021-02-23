using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Telldus
{
    public class Discover : IDisposable
    {
        private Task listener;
        private ushort port;
        private string broadcast;
        private Socket socket;
        public Discover(ushort port, string broadcast)
        {
            this.port = port;
            this.broadcast = broadcast;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            this.socket.ReceiveTimeout = 100;
            this.socket.SendTimeout = 100;
        }
        public void Send()
        {
            try
            {
                this.socket.SendTo(UTF8Encoding.UTF8.GetBytes("D"), new IPEndPoint(IPAddress.Parse(this.broadcast), this.port));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public Action<string, string> OnFoundDevice { get; set; }
        public void Start()
        {
            try
            {
                var listen = new IPEndPoint(IPAddress.Any, this.port);
                this.socket.Bind(listen);
                var buffer = new byte[1024];
                this.listener = Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var endPoint = listen as EndPoint;
                            int length = socket.ReceiveFrom(buffer, ref endPoint);
                            if (length == 0)
                                break;
                            var message = Encoding.UTF8.GetString(buffer, 0, length);
                            if (message != "D")
                            {
                                this.OnFoundDevice?.Invoke(((endPoint as IPEndPoint).Address).ToString(), message);
                            }
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot bind discover socket. Message {e.Message}");
            }
        }
        public void Stop()
        {
            this.socket?.Close();
            this.listener?.Wait();
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
                this.Stop();
                this.socket.Dispose();
                this.listener?.Dispose();
                this.socket = null;
                this.listener = null;
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Discover()
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
