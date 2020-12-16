namespace GrpcConsul
{
    public interface IEndpointStrategy
    {
        ServerCallInvoker Get(string serviceName);
        void Revoke(string serviceName, ServerCallInvoker failedCallInvoker);
    }
}