using System;
using System.Collections;
using System.Collections.Generic;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);
            _prompts = new Stack();
            _rand = new Random();
            _tunnel = new RequestSocket();
            _tunnel.Connect("tcp://127.0.0.1:5556");
        }

        private async Task OnMessage(object sender, MessageReceivedEventArgs eventArgs)
        {
            var fun = false;
            if (!(eventArgs.Message.Author.Id != _client.CurrentUser.Id
                  && (eventArgs.Message.Content.ToLower().Contains("nemu")
                      || eventArgs.Message.Content.ToLower().Contains(_client.CurrentUser.Name)
                      || eventArgs.Message.MentionedUsers.Contains(_client.CurrentUser))
                  )
                )
            {
                fun = _rand.Next(1, 10) == 1;
                if (!(fun
                      && eventArgs.Message.Author.Id != _client.CurrentUser.Id
                      && (eventArgs.Message.ChannelId == 566751794148016148 ||
                          eventArgs.Message.ChannelId == 633698411379556363)
                    ))
                {
                    return;
                } // TODO cleanup this sucks lol
            }

            try
            {
                using (eventArgs.Channel.BeginTyping())
                {
                    var oldMessages = await eventArgs.Channel.FetchMessagesAsync(limit: 5, RetrievalDirection.Before,
                        startFromId: eventArgs.MessageId, new DefaultRestRequestOptions());
                    List<String> history = new List<string>();
                    foreach (var oldMessage in oldMessages)
                    {
                        try
                        {
                            if (!String.IsNullOrWhiteSpace(oldMessage.Content))
                            {
                                history.Add("<|startoftext|>" + oldMessage.Content + "<|endoftext|>");
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    history.Reverse();

                    var genPrefix = String.Join("\n", history);
                    var genResponse = await GenerateMessage(genPrefix + "\n<|startoftext|>"
                                                                      + eventArgs.Message.Content
                                                                          .Replace(_client.CurrentUser.Name, "")
                                                                          .Replace("nemu", "")
                                                                          .Replace("  ", " ").Trim()
                                                                      + "<|endoftext|>\n<|startoftext|>");
                    if (String.IsNullOrWhiteSpace(genResponse))
                        genResponse = "_ _";
                    var msg = new LocalMessageBuilder()
                        .WithContent(genResponse);
                    if (!fun) // since fun==1 responses are unprovoked "comments" in a conversation remove the reply
                        msg.WithReply(eventArgs.MessageId, eventArgs.ChannelId, eventArgs.GuildId);
                    Logger.LogInformation("replying...");
                    await _client.SendMessageAsync(eventArgs.ChannelId, msg.Build(), new DefaultRestRequestOptions());
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