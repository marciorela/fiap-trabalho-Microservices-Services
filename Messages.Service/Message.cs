using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messages.Service.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace Messages.Service
{
    public class Message : IDisposable
    {
        private ServiceBusClient s_client;
        private ServiceBusAdministrationClient s_adminClient;
        private ServiceBusProcessor? processor;
        protected readonly string _subscriptionName = "";
        protected string _topicName = "";
        protected CreateRuleOptions _rule = new();

        public Message()
        {
            var config = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .Build();

            _subscriptionName = config.GetValue<string>("SubscriptionName");

            var connection = config.GetConnectionString("ServiceBus");

            s_client = new ServiceBusClient(connection);
            s_adminClient = new ServiceBusAdministrationClient(connection);

            Configure();

            Task.Run(async () => await CreateTopic(_topicName)).Wait();
            Task.Run(async () => await CreateSubscription(_topicName, _subscriptionName)).Wait();
            //Task.Run(async () => await CreateRules(_topicName, SubscriptionName)).Wait();
        }

        virtual protected void Configure()
        {
        }

        public async Task Send(object message)
        {
            var s_sender = s_client.CreateSender(_topicName);

            try
            {
                var bodySerialized = JsonConvert.SerializeObject(message);
                var bodyByteArray = Encoding.UTF8.GetBytes(bodySerialized);

                ServiceBusMessage msg = new()
                {
                    Body = new BinaryData(bodyByteArray),
                    MessageId = Guid.NewGuid().ToString(),
                    Subject = _subscriptionName
                };
                
                await s_sender.SendMessageAsync(msg);
            }
            finally
            {
                await s_sender.CloseAsync();
            }
        }

        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");
            await args.CompleteMessageAsync(args.Message);
        }
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public async Task Processa(MessageData msg)
        {
            await Receive(Processa);
        }

        public async Task Receive(Func<MessageData, Task> processMessage)
        {
            processor = s_client.CreateProcessor(_topicName, _subscriptionName);

            processor.ProcessMessageAsync += async (args) =>
            {
                var data = new MessageData()
                {
                    MessageId = Guid.Parse(args.Message.MessageId),
                    CorrelationId = (!string.IsNullOrWhiteSpace(args.Message.CorrelationId) ? Guid.Parse(args.Message.CorrelationId) : null),
                    Body = args.Message.Body
                };

                await processMessage(data);

                await args.CompleteMessageAsync(args.Message);
            };

            processor.ProcessErrorAsync += ErrorHandler;
        
            await processor.StartProcessingAsync();
        }

        private async Task CreateRules(string topicName, string subscriptionName)
        {
            const string filterName = "filter-store";

            var found = false;
            var rules = s_adminClient.GetRulesAsync(topicName, subscriptionName);
            await foreach (var rule in rules)
            {
                if (rule.Name == RuleProperties.DefaultRuleName)
                {
                    await s_adminClient.DeleteRuleAsync(topicName, subscriptionName, RuleProperties.DefaultRuleName);
                }
                else if (rule.Name == filterName)
                {
                    found = true;
                }
            }

            if (!found)
            {
                await s_adminClient.CreateRuleAsync(_topicName, _subscriptionName, new CreateRuleOptions("filter-store", new CorrelationRuleFilter() { Subject = _subscriptionName }));
            }
        }

        private async Task CreateTopic(string topicName)
        {
            var found = false;
            var topics = s_adminClient.GetTopicsAsync();
            await foreach (var topic in topics)
            {
                if (topic.Name.ToLower() == topicName.ToLower())
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                await s_adminClient.CreateTopicAsync(topicName.ToLower());
            }
        }

        private async Task CreateSubscription(string topicName, string subscriptionName)
        {
            var found = false;
            var subs = s_adminClient.GetSubscriptionsAsync(topicName);
            await foreach (var sub in subs)
            {
                if (sub.SubscriptionName == subscriptionName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                await s_adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicName, subscriptionName), _rule);
            }
        }

        public async void Dispose()
        {
            await s_client.DisposeAsync();
            if (processor is not null)
            {
                await processor.StopProcessingAsync();
            }
        }
    }
}