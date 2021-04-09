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
            return;
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
            _tunnel = new RequestSocket();
            _tunnel.Connect("tcp://127.0.0.1:5556");
        }

        private async Task OnMessage(object sender, MessageReceivedEventArgs eventArgs)
        {
            // _rand.Next(1, 5) != 1 || 
            if (eventArgs.ChannelId != 566751794148016148 || !eventArgs.Message.Content.Contains("nemuri") || eventArgs.Message.Author == _client.CurrentUser) // randomizer? lol
                return;
            
            
            
            try
            {
                var genResponse = await GenerateMessage(eventArgs.Message.Content.Replace("nemuri", ""));
                if (String.IsNullOrWhiteSpace(genResponse))
                    genResponse = "_ _";
                var msg = new LocalMessageBuilder()
                    .WithContent(genResponse)
                    .WithReply(eventArgs.MessageId, eventArgs.ChannelId, eventArgs.GuildId)
                    .Build();
                Logger.LogInformation("replying...");
                using (eventArgs.Channel.BeginTyping())
                {
                    await _client.SendMessageAsync(eventArgs.ChannelId, msg, new DefaultRestRequestOptions());
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, String.Empty);
            }
        }

        public async Task<string> GenerateMessage(string prefixArg)
        {
            Logger.LogInformation("MessageReceived fired, generating response...");
            //if (_busySocket)
            //    _prompts.Push(prefixArg);
            //_busySocket = true;
            _tunnel.SendFrame(prefixArg);
            var response = _tunnel.ReceiveFrameString();
            Logger.LogInformation("received response...");
            //_busySocket = false;
            return response;
        }
    }
}