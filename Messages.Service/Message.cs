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
        protected readonly string SubscriptionName = ""; // = "paulista_store";
        protected string _topicName = "";
        protected CreateRuleOptions _rule = new();

        public Message(string storeName)
        {
            SubscriptionName = storeName;

            var config = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .Build();

            var connection = config.GetConnectionString("ServiceBus");

            s_client = new ServiceBusClient(connection);
            s_adminClient = new ServiceBusAdministrationClient(connection);

            Configure();

            Task.Run(async () => await CreateTopic(_topicName)).Wait();
            Task.Run(async () => await CreateSubscription(_topicName, SubscriptionName)).Wait();
            Task.Run(async () => await CreateRules(_topicName, SubscriptionName)).Wait();
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
                    Subject = SubscriptionName
                };
                
                await s_sender.SendMessageAsync(msg);
            }
            finally
            {
                await s_sender.CloseAsync();
            }
        }

        public async Task<MessageData?> Receive()
        {
            await using ServiceBusReceiver s_receiver = s_client.CreateReceiver(_topicName, SubscriptionName,
                new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

            try
            {
                var receivedMessage = await s_receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));
                if (receivedMessage != null)
                {
                    return new MessageData()
                    {
                        MessageId = Guid.Parse(receivedMessage.MessageId),
                        CorrelationId = (!string.IsNullOrWhiteSpace(receivedMessage.CorrelationId) ? Guid.Parse(receivedMessage.CorrelationId) : null),
                        Body = receivedMessage.Body
                    };
                }

                return null;
            }
            finally
            {
                await s_receiver.CloseAsync();
            }
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
                await s_adminClient.CreateRuleAsync(_topicName, SubscriptionName, new CreateRuleOptions("filter-store", new CorrelationRuleFilter() { Subject = SubscriptionName }));
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
        }
    }
}