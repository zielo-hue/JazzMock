using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Hosting;
using Disqord.Rest;
using Disqord.Rest.Default;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Qmmands;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace JazzMock.Services
{
    public class GptGeneratorService : DiscordClientService
    {
        private string RUN_NAME;
        private Stack _prompts;
        private Random _rand;
        private RequestSocket _tunnel;
        private Boolean _busySocket;
        private DiscordClientBase _client;
        
        public GptGeneratorService(ILogger<GptGeneratorService> logger, DiscordClientBase client) : base(logger, client)
        {
            // _ = NetmqTest();
            _client = client;
            client.MessageReceived += OnMessage;
        }

        private async Task NetmqTest()
        {
            using var requestSocket = new RequestSocket();
            requestSocket.Connect("tcp://127.0.0.1:5556");
            await Task.Run(() =>
            {
                while (true)
                {
                    Task.Delay(10000);
                    requestSocket.SendFrame("Hello");
                    var msg = requestSocket.ReceiveFrameString();
                    Logger.LogInformation(msg);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);
            _ = NetmqTest();
            _prompts = new Stack();
            _rand = new Random();
        }

        private async Task OnMessage(object sender, MessageReceivedEventArgs eventArgs)
        {
            if (_rand.Next(1, 5) != 1) // randomizer? lol
                return;
            var genResponse = await GenerateMessage(eventArgs.Message.Content);
            var msg = new LocalMessageBuilder()
                .WithContent(genResponse)
                .WithReply(eventArgs.MessageId, eventArgs.ChannelId, eventArgs.GuildId)
                .Build();
            await _client.SendMessageAsync(eventArgs.ChannelId, msg, new DefaultRestRequestOptions());
        }

        public async Task<string> GenerateMessage(string prefixArg)
        {
            if (_busySocket)
                _prompts.Push(prefixArg);
            _busySocket = true;
            _tunnel.SendFrame(prefixArg);
            var response = _tunnel.ReceiveFrameString();
            _busySocket = false;
            return response;
        }
    }
}