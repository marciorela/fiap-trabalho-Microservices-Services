namespace Messages.Service.Messages
{
    public class MessageUserWithLessOffer : Message
    {

        protected override void Configure()
        {
            _topicName = "UserWithLessOffer";
        }
    }
}
