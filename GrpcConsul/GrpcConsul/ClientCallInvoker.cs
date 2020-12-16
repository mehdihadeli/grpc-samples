using System;
using Grpc.Core;

namespace GrpcConsul
{
    internal sealed class ClientCallInvoker : CallInvoker
    {
        private readonly IEndpointStrategy _endpointStrategy;
        private readonly int _maxRetry;

        public ClientCallInvoker(IEndpointStrategy endpointStrategy, int maxRetry = 0)
        {
            _endpointStrategy = endpointStrategy;
            _maxRetry = maxRetry;
        }

        private TResponse Call<TResponse>(string serviceName, Func<CallInvoker, TResponse> call, int retryLeft)
        {
            while (true)
            {
                var callInvoker = _endpointStrategy.Get(serviceName);
                try
                {
                    return call(callInvoker);
                }
                catch (RpcException ex)
                {
                    // forget channel if unavailable
                    if (ex.Status.StatusCode == StatusCode.Unavailable)
                    {
                        _endpointStrategy.Revoke(serviceName, callInvoker);
                    }

                    if (0 > --retryLeft)
                    {
                        throw;
                    }
                }
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.BlockingUnaryCall(method, host, options, request), _maxRetry);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.AsyncUnaryCall(method, host, options, request), _maxRetry);
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.AsyncServerStreamingCall(method, host, options, request), _maxRetry);
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call(method.ServiceName, ci => ci.AsyncClientStreamingCall(method, host, options), _maxRetry);
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call(method.ServiceName, ci => ci.AsyncDuplexStreamingCall(method, host, options), _maxRetry);
        }
    }
}