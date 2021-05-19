using System;
using System.Net;

namespace Encodeous.Musii.Network
{
    public class ProxyWrapper : IDisposable
    {
        private Action<IPEndPoint> _disposeAction;
        public IPEndPoint EndPoint { get; }

        public ProxyWrapper(Action<IPEndPoint> disposeAction, IPEndPoint endPoint)
        {
            _disposeAction = disposeAction;
            EndPoint = endPoint;
        }

        public void Dispose()
        {
            _disposeAction.Invoke(EndPoint);
        }
    }
}