using System;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcConsul;
using Helloworld;

namespace Server
{
    internal class GreeterImpl : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            var port = int.Parse(args[0]);
            var serviceDiscovery = new ServiceDiscovery();
            var server = new Grpc.Core.Server
                             {
                                 Services = { Greeter.BindService(new GreeterImpl()) },
                                 Ports = { new ServerPort(serviceDiscovery.GetHostName(), port, ServerCredentials.Insecure) }
                             };


            server.Start();
            using (serviceDiscovery.RegisterService(Greeter.Descriptor.FullName, port))
            {
                Console.WriteLine($"{Greeter.Descriptor.FullName} service listening on port {port}");
                Console.WriteLine("Press ENTER to exit");
                Console.ReadLine();
            }

            server.ShutdownAsync().Wait();
        }
    }
}