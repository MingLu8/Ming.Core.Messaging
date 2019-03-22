using Microsoft.Extensions.Logging;

namespace Ming.Core.Messaging
{
    public class DefaultQueueClientFactory : IQueueClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DefaultQueueClientFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        public virtual ISimpleQueueClient Create(string connectionString, string queueName, string messageVersion = null)
        {
            return new DefaultQueueClient(connectionString, queueName, messageVersion, _loggerFactory);
        }
    }
}
