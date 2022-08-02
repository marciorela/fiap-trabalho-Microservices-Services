using Azure.Messaging.ServiceBus.Administration;
using Geekburger.Order.Contract.Messages;
using Geekburger.Order.Data.Repositories;
using GeekBurguer.UI.Contract;
using Messages.Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages.Service.Messages
{
    public class MessageUserWithLessOffer : Message
    {
        public MessageUserWithLessOffer(string storeName) : base(storeName)
        {
        }

        protected override void Configure()
        {
            _topicName = "UserWithLessOffer";
        }
    }
}
