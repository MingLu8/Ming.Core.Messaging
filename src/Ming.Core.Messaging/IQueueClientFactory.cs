namespace Ming.Core.Messaging
{
    public interface IQueueClientFactory
    {
        ISimpleQueueClient Create(string connectionString, string queueName, string messageVersion = null);
    }
}
