using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Hosting;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace JazzMock.Services
{
    public class GptGeneratorService : DiscordClientService
    {
        private string RUN_NAME;
        public GptGeneratorService(ILogger<GptGeneratorService> logger, DiscordClientBase client) : base(logger, client)
        {
            using var requestSocket = new RequestSocket();
            requestSocket.Connect("tcp://127.0.0.1:5556");
            requestSocket.SendFrame("Hello");
            var msg = requestSocket.ReceiveFrameString();
            Console.WriteLine($"From server: {msg}");
        }

        public async Task<string[]> GenerateMessage(string prefixArg)
        {
            return new []{"lol"};
        }
    }
}