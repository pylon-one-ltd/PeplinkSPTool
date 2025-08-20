using System.Net;
using System.Text;
using System.Net.Sockets;

namespace PeplinkSPTool.Classes
{
    internal class LightweightHttpServer
    {
        private readonly TcpListener tcpListener = new(IPAddress.Any, 5999);
        private bool IsRunning = true;
        
        public Task StartServer()
        {
            return Task.Run(async() =>
            {
                tcpListener.Start();
                const int bufferSize = 512;
                var bytes = new byte[bufferSize];
                List<TcpClient> clients = [];
                while( IsRunning )
                {
                    // Cull closed clients.
                    if( clients.Any(c => !c.Connected) )
                    {
                        clients = clients.Where(c => c.Connected).ToList();
                    }

                    // Accept the next incoming request from tcpListener.
                    if( tcpListener.Pending() )
                    {
                        var client = await tcpListener.AcceptTcpClientAsync();
                        clients.Add(client);
                    }

                    foreach( var client in clients )
                    {
                        if( client.Available > 0 )
                        {
                            var stream = client.GetStream();
                            while( client.Connected && stream.DataAvailable )
                            {
                                // Technically this process splits the data on the bufferSize boundaries, which could in some
                                // extreme edge cases cause us to fail to recognize one of the requests that we care about.
                                // However in practice this hasn't been observed, so for simplicity, we'll leave it as is until
                                // we have a good test case to ensure we handle it correctly and safely when we really need to.
                                if( stream.Read(bytes, 0, bufferSize) > 0 )
                                {
                                    var received = Encoding.UTF8.GetString(bytes);
                                    var parts = received.Split('\r', '\n');
                                    foreach( var part in parts )
                                    {
                                        var httpPartIndex = part.LastIndexOf(" HTTP/", StringComparison.Ordinal);
                                        if( part.Contains(" HTTP/") && part.StartsWith("GET /reply"))
                                        {
                                            stream.Write(GetAuthReply(part[4..httpPartIndex]));
                                            stream.Flush();
                                            client.Close();
                                        }
                                        else if( part.Contains(" HTTP/") )
                                        {
                                            stream.Write(GenerateResponse(HttpStatusCode.NotFound, "Page not found"));
                                            stream.Flush();
                                            client.Close();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(10);
                }
            });
        }
        
        public void StopServer()
        {
            IsRunning = false;
            tcpListener.Stop();
        }
        
        public byte[] GetAuthReply(string FullURL)
        {
            if( !FullURL.StartsWith("/reply?code=") ) return GenerateResponse(HttpStatusCode.BadRequest, "Missing code parameter");
            var code = FullURL.Split('=')[1];
            if( string.IsNullOrEmpty(code) )
            {
                return GenerateResponse(HttpStatusCode.BadRequest, "Peplink did not respond with a valid code");
            }
            
            Program.PeplinkChallengeCode = code;
            return GenerateResponse(HttpStatusCode.OK, "Peplink authentication successful. You may now close this window.");
        }
        
        private byte[] GenerateResponse(HttpStatusCode statusCode, string Message)
        {
            var ResponseStream = $"""
HTTP/1.1 {(int)statusCode} {Enum.GetName(statusCode)}
Content-Type: text/plain; charset=UTF-8
Content-Length: {Message.Length}

{Message}
""";
            return Encoding.UTF8.GetBytes(ResponseStream);
        }
    }
}

