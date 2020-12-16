using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Consul;

namespace GrpcConsul
{
    public sealed class ServiceDiscovery
    {
        private readonly ConsulClient _client;
        private readonly ConcurrentDictionary<string, DateTime> _blacklist = new ConcurrentDictionary<string, DateTime>();

        public ServiceDiscovery()
        {
            _client = new ConsulClient();
        }

        public string GetHostName()
        {
            return Dns.GetHostName();
        }

        public Entry RegisterService(string name, int port)
        {
            var hostName = Dns.GetHostName();
            var serviceId = $"{hostName}-{name}-{port}";
            var checkId = $"{hostName}-{port}";

            var acr = new AgentCheckRegistration
                          {
                              TCP = $"{hostName}:{port}",
                              Name = checkId,
                              ID = checkId,
                              Interval = ConsulConfig.CheckInterval,
                              DeregisterCriticalServiceAfter = ConsulConfig.CriticalInterval
                          };

            var asr = new AgentServiceRegistration
                          {
                              Address = hostName,
                              ID = serviceId,
                              Name = name,
                              Port = port,
                              Check = acr
                          };

            var res = _client.Agent.ServiceRegister(asr).Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to register service {name} on port {port}");
            }

            return new Entry(this, name, port, serviceId);
        }

        public void UnregisterService(string serviceId)
        {
            var res = _client.Agent.ServiceDeregister(serviceId).Result;
        }

        public string FindServiceEndpoint(string name)
        {
            var res = _client.Agent.Services().Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to query services");
            }

            var rnd = new Random();
            var now = DateTime.UtcNow;
            var targets = res.Response
                             .Values
                             .Where(x => x.Service == name)
                             .Select(x => $"{x.Address}:{x.Port}")
                             .ToList();
            while (0 < targets.Count)
            {
                var choice = rnd.Next(targets.Count);
                var target = targets[choice];
                DateTime lastFailure;
                if (_blacklist.TryGetValue(target, out lastFailure))
                {
                    // within blacklist period ?
                    if (now - lastFailure < ConsulConfig.BlacklistPeriod)
                    {
                        targets.RemoveAt(choice);
                        continue;
                    }

                    // blacklist timeout elapsed
                    _blacklist.TryRemove(target, out lastFailure);
                }

                return target;
            }

            throw new ApplicationException($"Can't find service {name}");
        }

        public void Blacklist(string target)
        {
            var now = DateTime.UtcNow;
            _blacklist.AddOrUpdate(target, k => now, (k, old) => now);
        }

        public sealed class Entry : IDisposable
        {
            private readonly ServiceDiscovery _serviceDiscovery;

            internal Entry(ServiceDiscovery serviceDiscovery, string serviceName, int port, string serviceId)
            {
                ServiceName = serviceName;
                Port = port;
                ServiceId = serviceId;
                _serviceDiscovery = serviceDiscovery;
            }

            public string ServiceName { get; }
            public int Port { get; }
            public string ServiceId { get; }

            public void Dispose()
            {
                _serviceDiscovery.UnregisterService(ServiceId);
            }
        }
    }
}