# Sample for Grpc + Consul

  * Server register a service in Consul
  * Client tries to invoke a service with Consul as resolver
  * Services are checked by Consul
  * Client can blacklist endpoints for a while and recover then
  * Client can choose an endpoint strategy and retry

# How to run ?

  1. Build solution.
  2. Run consul (consul.exe agent -dev)
  3. Run servers (server.exe 50051 && server.exe 50052)
  4. Run client (client.exe)
  
# How this works ?  

Hook into CallInvoker and select random endpoint.

# Notice

This is a POC only, this is an incomplete work and can't be considered reliable.
Do what you want with this !