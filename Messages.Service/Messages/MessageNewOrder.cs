namespace Messages.Service.Messages
{
    public class MessageNewOrder : Message
    {
        protected override void Configure()
        {
            _topicName = "NewOrder";
        }
    }
}
