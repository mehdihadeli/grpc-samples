using System;
using System.Threading;
using System.Threading.Tasks;
using GrpcConsul;
using Helloworld;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serviceDiscovery = new ServiceDiscovery();
            var endpointStrategy = new StickyEndpointStrategy(serviceDiscovery);
            var clientFactory = new ClientFactory(endpointStrategy);

            var tasks = new Task[3];
            for (var i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = Task.Factory.StartNew(x => RunClientTest(clientFactory), null);
            }

            Console.ReadLine();
        }

        private static void RunClientTest(ClientFactory clientFactory)
        {
            var client = clientFactory.Get<Greeter.GreeterClient>();
            var rnd = new Random();

            var attempt = 0;
            while (true)
            {
                ++attempt;

                try
                {
                    var reply = client.SayHello(new HelloRequest { Name = $"{attempt}" });
                    Console.WriteLine($"Success attempt: {attempt} thread: {Thread.CurrentThread.ManagedThreadId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failure attempt: {attempt} thread: {Thread.CurrentThread.ManagedThreadId} error: {ex.Message}");
                }

                Thread.Sleep(rnd.Next(1000));
            }
        }
    }
}