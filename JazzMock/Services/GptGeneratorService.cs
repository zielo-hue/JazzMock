using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
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
        private Random _rand;
        private RequestSocket _tunnel;
        private DiscordClientBase _client;
        private uint _queue;
        private readonly Channel<MessageReceivedEventArgs> _channel;
        private List<string> _ignoreList;
        
        public GptGeneratorService(ILogger<GptGeneratorService> logger, DiscordClientBase client) : base(logger, client)
        {
            _client = client;
            _channel = Channel.CreateUnbounded<MessageReceivedEventArgs>();
            _rand = new Random();
            _tunnel = new RequestSocket();
            _tunnel.Connect("tcp://127.0.0.1:5556");
            _queue = 0;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);
            _ignoreList = new List<string>
            {
                "nemu",
                _client.CurrentUser.Name,
                _client.CurrentUser.Id.ToString(),
            };
            await foreach (var e in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using (e.Channel.BeginTyping())
                    {
                        var oldMessages = await e.Channel.FetchMessagesAsync(limit: 4, RetrievalDirection.Before,
                            startFromId: e.MessageId, new DefaultRestRequestOptions()); // get old messages for context
                        List<String> history = new List<string>();
                        foreach (var oldMessage in oldMessages)
                        {
                            history.Add("<|startoftext|>" + oldMessage.Content + "<|endoftext|>");
                        }
                        history.Reverse();

                        var genPrefix = String.Join("\n", history);

                        var fun = true;
                        foreach (var keyword in _ignoreList)
                        {
                            if (e.Message.Content.Contains(keyword))
                            {
                                fun = false;
                                break;
                            }
                        }
                        
                        var genResponse = await GenerateMessage(genPrefix + "\n<|startoftext|>"
                                                                          + e.Message.Content
                                                                              .Replace(_client.CurrentUser.Name, "")
                                                                              .Replace("  ", " ").Trim()
                                                                          + "<|endoftext|>\n<|startoftext|>");
                        if (String.IsNullOrWhiteSpace(genResponse))
                            genResponse = "_ _";
                        var msg = new LocalMessageBuilder()
                            .WithContent(genResponse);
                        if (!fun) // since fun responses are unprovoked "comments" in a conversation remove the reply
                            msg.WithReply(e.MessageId, e.ChannelId, e.GuildId);
                        Logger.LogInformation("replying...");
                        await _client.SendMessageAsync(e.ChannelId, msg.Build(), new DefaultRestRequestOptions());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, String.Empty);
                }
            }
        }

        protected override ValueTask OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            var fun = _rand.Next(1, 20) == 1; // fun value is used to determine whether the bot should answer unprovoked or not
            if (IgnoreMessage(fun, eventArgs))
                return default;
            
            return _channel.Writer.WriteAsync(eventArgs);
            
        }

        public async Task<string> GenerateMessage(string prefixArg, byte settings = 0b0000)
        {
            _queue++;
            if (_queue > 10)
            {
                throw new TaskCanceledException("obvious spam is obvious"); // obvious spam is obvious
            }
            Logger.LogInformation("MessageReceived fired, generating response...");
            var message = new NetMQMessage();
            message.Append(settings);
            message.Append(prefixArg);
            
            _tunnel.TrySendMultipartMessage(message);
            
            var response = _tunnel.ReceiveFrameString();
            Logger.LogInformation("received response...");
            if (settings == 1) // genconvo?
                response = response.Replace("<|startoftext|>", ">")
                    .Replace("<|endoftext|>", "");
            _queue--;
            return response;
        }

        private bool IgnoreMessage(bool fun, MessageReceivedEventArgs eventArgs)
        {
            if (!(eventArgs.Message.Author.Id != _client.CurrentUser.Id
                  && (eventArgs.Message.Content.ToLower().Contains("nemu")
                      || eventArgs.Message.Content.ToLower().Contains(_client.CurrentUser.Name)
                      || eventArgs.Message.MentionedUsers.Contains(_client.CurrentUser)) // this does not work
                )
            ) // check that the message has trigger words or if the message is by the bot itself. if not, continue on
            {
                if (!(fun
                      && eventArgs.Message.Author.Id != _client.CurrentUser.Id
                      && (eventArgs.Message.ChannelId == 839699838051418123)
                    )) // check that the message was sent in hardcoded guilds, and make sure again that the message wasn't sent by the bot (to be really sure)
                {
                    return true;
                } // TODO cleanup this sucks lol
            }

            return false;
        }
    }
}