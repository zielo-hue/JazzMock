﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly Channel<MessageReceivedEventArgs> _channel;
        private List<string> _incompleteKeywords;
        private List<string> _ignoreList;
        
        private static readonly Regex LinkRegex = new(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/=]*)", RegexOptions.Compiled);
        
        public GptGeneratorService(ILogger<GptGeneratorService> logger, DiscordClientBase client) : base(logger, client)
        {
            _client = client;
            _channel = Channel.CreateUnbounded<MessageReceivedEventArgs>();
            _rand = new Random();
            _tunnel = new RequestSocket();
            _tunnel.Connect("tcp://127.0.0.1:5556");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);
            await Client.SetPresenceAsync(new LocalActivity("counter strike", ActivityType.Playing));
            _ignoreList = new List<string> {_client.CurrentUser.Name, _client.CurrentUser.Id.ToString(),};
            _incompleteKeywords = new List<string> {"also", "like", "but", "i mean"}; // hardcoded keywords omegalul
            
            await foreach (var e in _channel.Reader.ReadAllAsync(stoppingToken))
                // this foreach waits until a new object is added to process (very cool!)
            {
                try
                {
                    using (e.Channel.BeginTyping())
                    {
                        var lookupLimit = 4;
                        if (e.Message.Content.Contains("?") || e.Message.Content.Contains("!"))
                            lookupLimit = 0; // if the message is a question dont get distracted by the context
                        
                        var oldMessages = await e.Channel.FetchMessagesAsync(limit: lookupLimit, RetrievalDirection.Before,
                            startFromId: e.MessageId,
                            new DefaultRestRequestOptions()); // get old messages for context
                        List<String> history = new List<string>();
                        foreach (var oldMessage in oldMessages)
                        {
                            var sanitizedMessage = LinkRegex.Replace(oldMessage.Content + " ", String.Empty).Trim();
                            if (sanitizedMessage != String.Empty)
                                history.Add("<|startoftext|>" + sanitizedMessage + "<|endoftext|>");
                        }

                        history.Reverse();
                        var genPrefix = String.Join("\n", history);

                        // determine if the queued request was unprovoked
                        var fun = true;
                        foreach (var keyword in _ignoreList)
                        {
                            if (e.Message.Content.ToLower().Contains(keyword.ToLower()))
                            {
                                fun = false;
                                break;
                            }
                        }
                        
                        var trimmedPrompt = Regex.Replace(e.Message.Content, _client.CurrentUser.Name, String.Empty,
                            RegexOptions.IgnoreCase);
                        var truncateStringIndex = trimmedPrompt.LastIndexOf("<|endoftext|>", StringComparison.Ordinal);
                        if (truncateStringIndex != -1)
                            trimmedPrompt = trimmedPrompt.Substring(truncateStringIndex + 13);
                        var genResponse = await GenerateMessage(genPrefix + "\n<|startoftext|>"
                                                                          + trimmedPrompt
                                                                              .Replace("  ", " ").Trim()
                                                                          + "<|endoftext|>\n<|startoftext|>");
                        
                        if (IsIncomplete(genResponse))
                            genResponse += "\n" + await GenerateMessage("<|startoftext|>" + genResponse
                                + "<|endoftext|>\n<|startoftext|>");
                        if (String.IsNullOrWhiteSpace(genResponse))
                            genResponse = "_ _";
                        
                        if (genResponse.Length > 2000)
                        {
                            await _client.SendMessageAsync(e.ChannelId,
                                new LocalMessage().WithContent("...I'm not going to bother.")
                                    .WithReply(e.MessageId, e.ChannelId, e.GuildId),
                                new DefaultRestRequestOptions());
                            return;
                        }

                        var msg = new LocalMessage()
                            .WithContent(genResponse);
                        if (!fun) // since fun responses are unprovoked "comments" in a conversation remove the reply
                            msg.WithReply(e.MessageId, e.ChannelId, e.GuildId);
                        Logger.LogInformation("replying...");
                        await _client.SendMessageAsync(e.ChannelId, msg, new DefaultRestRequestOptions());
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
            var fun = _rand.Next(1, 31) == 1; // fun value is used to determine whether the bot should answer unprovoked or not
            if (IgnoreMessage(fun, eventArgs))
                return default;
            
            return _channel.Writer.WriteAsync(eventArgs);
            
        }

        public async Task<string> GenerateMessage(string prefixArg, byte settings = 0b0000)
        {
            if (_channel.Reader.Count > 6)
            {
                throw new TaskCanceledException("obvious spam is obvious"); // obvious spam is obvious
            }
            Logger.LogInformation("MessageReceived fired, generating response...");
            var message = new NetMQMessage();
            message.Append(settings);
            message.Append(prefixArg);
            
            _tunnel.TrySendMultipartMessage(message);
            
            var response = Regex.Unescape(_tunnel.ReceiveFrameString());
            Logger.LogInformation("received response...");
            if (settings == 1) // genconvo?
                response = response.Replace("<|startoftext|>", ">")
                    .Replace("<|endoftext|>", "");
            return response;
        }

        private bool IgnoreMessage(bool fun, MessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Message.ChannelId == 858762692806312017)
                return true;
            
            
            
            if (!(eventArgs.Message.Author.Id != _client.CurrentUser.Id
                  && (eventArgs.Message.Content.ToLower().Contains(_client.CurrentUser.Name.ToLower())
                      || eventArgs.Message.MentionedUsers.Contains(_client.CurrentUser)) // this does not work
                )
            ) // check that the message has trigger words or if the message is by the bot itself. if not, continue on
            {
                if (!(fun
                      && eventArgs.Message.Author.Id != _client.CurrentUser.Id
                      && (eventArgs.Message.GetChannel().CategoryId == 837069822406688772)
                    )) // check that the message was sent in hardcoded guilds, and make sure again that the message wasn't sent by the bot (to be really sure)
                {
                    return true;
                } // TODO cleanup this sucks lol
            }

            return false;
        }

        private bool IsIncomplete(string message)
        {
            foreach (var keyword in _incompleteKeywords)
            {
                if (message.ToLower().EndsWith(keyword))
                    return true;
            }

            return false;
        }
    }
}