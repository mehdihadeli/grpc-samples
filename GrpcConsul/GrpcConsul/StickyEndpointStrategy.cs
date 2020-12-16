using System.Collections.Concurrent;
using System.Collections.Generic;
using Grpc.Core;

namespace GrpcConsul
{
    public class StickyEndpointStrategy : IEndpointStrategy
    {
        private readonly object _lock = new object();
        private readonly ServiceDiscovery _serviceDiscovery;
        private readonly ConcurrentDictionary<string, ServerCallInvoker> _invokers = new ConcurrentDictionary<string, ServerCallInvoker>();
        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();

        public StickyEndpointStrategy(ServiceDiscovery serviceDiscovery)
        {
            _serviceDiscovery = serviceDiscovery;
        }

        public ServerCallInvoker Get(string serviceName)
        {
            // find callInvoker first if any (fast path)
            ServerCallInvoker callInvoker;
            if (_invokers.TryGetValue(serviceName, out callInvoker))
            {
                return callInvoker;
            }

            // no luck (slow path): either no call invoker available or a shutdown is in progress
            lock (_lock)
            {
                // this is double-check lock
                if (_invokers.TryGetValue(serviceName, out callInvoker))
                {
                    return callInvoker;
                }

                // find a (shared) channel for target if any
                var target = _serviceDiscovery.FindServiceEndpoint(serviceName);
                Channel channel;
                if (!_channels.TryGetValue(target, out channel))
                {
                    channel = new Channel(target, ChannelCredentials.Insecure);
                    _channels.Add(target, channel);
                }

                // build a new call invoker + channel
                callInvoker = new ServerCallInvoker(channel);
                _invokers.TryAdd(serviceName, callInvoker);

                return callInvoker;
            }
        }

        public void Revoke(string serviceName, ServerCallInvoker failedCallInvoker)
        {
            lock (_lock)
            {
                // only destroy the call invoker if & only if it is still published (first arrived wins)
                ServerCallInvoker callInvoker;
                if (!_invokers.TryGetValue(serviceName, out callInvoker) || !ReferenceEquals(callInvoker, failedCallInvoker))
                {
                    return;
                }
                _invokers.TryRemove(serviceName, out callInvoker);

                // shutdown the channel
                var failedChannel = failedCallInvoker.Channel;
                Channel channel;
                if (_channels.TryGetValue(failedChannel.Target, out channel) && ReferenceEquals(channel, failedChannel))
                {
                    _channels.Remove(failedChannel.Target);
                    _serviceDiscovery.Blacklist(failedChannel.Target);
                }

                failedChannel.ShutdownAsync();
            }
        }
    }
}