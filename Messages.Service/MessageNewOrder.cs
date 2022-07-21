using Azure.Messaging.ServiceBus.Administration;
using Geekburger.Order.Contract.Messages;
using Messages.Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages.Service
{
    public class MessageNewOrder : Message
    {
        public MessageNewOrder(string storeName) : base(storeName)
        {
        }

        protected override void Configure()
        {
            _topicName = "NewOrder";
        }

        public override void Process(MessageData received)
        {
            Console.WriteLine("Recebido:");
            Console.WriteLine(received.MessageId.ToString());

            var x = Encoding.UTF8.GetString(received.Body);
            var y = JsonConvert.DeserializeObject<OrderChanged>(x);

            Console.WriteLine(y.OrderId);
            Console.WriteLine(y.State);
            Console.WriteLine(y.StoreName);
            Console.WriteLine(SubscriptionName);
        }
    }
}
