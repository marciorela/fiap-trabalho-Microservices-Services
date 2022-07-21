using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages.Service.Models
{
    public class MessageData
    {
        public Guid MessageId { get; set; }

        public Guid? CorrelationId { get; set; }

        public BinaryData? Body { get; set; }
    }
}
