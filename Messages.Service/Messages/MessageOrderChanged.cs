namespace Messages.Service.Messages
{
    public class MessageOrderChanged : Message
    {
        protected override void Configure()
        {
            _topicName = "OrderChanged";
        }
    }
}
