using System;

namespace GrpcConsul
{
    public class ClientFactory
    {
        private readonly ClientCallInvoker _callInvoker;

        public ClientFactory(IEndpointStrategy strategy)
        {
            _callInvoker = new ClientCallInvoker(strategy, 1);
        }

        public T Get<T>()
        {
            var client = (T) Activator.CreateInstance(typeof(T), _callInvoker);
            return client;
        }
    }
}