using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace Ming.Core.Messaging
{
    public class DefaultDeadLetterQueueClient : ISimpleDeadletterQueueClient
    {
        private readonly ILoggerFactory _loggerFactory;

        private ILogger _logger;
        private IQueueClient _receiverQueueClient;

        public virtual string ConnectionString { get; set; }

        /// <summary>
        /// queue name, not the dead letter queue.
        /// </summary>
        public virtual string QueueName { get; }

        protected virtual ILogger Logger => _logger ?? (_logger = _loggerFactory?.CreateLogger<DefaultDeadLetterQueueClient>());

        protected virtual IQueueClient ReceriverQueueClient => _receiverQueueClient ?? (_receiverQueueClient = new QueueClient(ConnectionString, QueueName));

        public DefaultDeadLetterQueueClient(string connectionString, string queueName, ILoggerFactory loggerFactory)
        {
            ConnectionString = connectionString;
            QueueName = queueName;
            _loggerFactory = loggerFactory;
        }


        public async virtual Task DeleteAllDeadLettersAsync()
        {
            _logger?.LogDebug($"Sending message to {QueueName}.");
            var deadQueuePath = EntityNameHelper.FormatDeadLetterPath(QueueName);
            var deadQueueReceiver = new MessageReceiver(ConnectionString, deadQueuePath);
            var messages = await deadQueueReceiver.ReceiveAsync(100, new System.TimeSpan(0, 0, 10));
            while (messages != null && messages.Any())
            {
                await deadQueueReceiver.CompleteAsync(messages.Select(a=>a.SystemProperties.LockToken));
                messages = await deadQueueReceiver.ReceiveAsync(100, new System.TimeSpan(0, 0, 10));
            }
        }

        public async virtual Task RequeueAllDeadLettersAsync(string filter = null)
        {
            _logger?.LogDebug($"Requeuing messages to {QueueName}.");
            var queueClient = new QueueClient(ConnectionString, QueueName);
            var deadQueuePath = EntityNameHelper.FormatDeadLetterPath(QueueName);
            var deadQueueReceiver = new MessageReceiver(ConnectionString, deadQueuePath);
            var messages = await deadQueueReceiver.ReceiveAsync(100, new System.TimeSpan(0, 0, 10));
            var remainingMessages = new List<Message>();
            var targetMessages = new List<Message>();
            while (messages != null && messages.Any())
            {
                targetMessages.AddRange(string.IsNullOrWhiteSpace(filter) ? messages.ToList() : messages.AsQueryable().Where(filter).ToList());
                remainingMessages.AddRange(messages.Where(a => !targetMessages.Exists(b=>b.SystemProperties.LockToken == a.SystemProperties.LockToken)));              
                messages = await deadQueueReceiver.ReceiveAsync(100, new System.TimeSpan(0, 0, 10));
            }

            remainingMessages.ForEach(a => deadQueueReceiver.AbandonAsync(a.SystemProperties.LockToken));
            targetMessages.ToList().ForEach(async a =>
            {
                await queueClient.SendAsync(a.Clone());
                await deadQueueReceiver.CompleteAsync(a.SystemProperties.LockToken);
            });
        }

        public async Task<IEnumerable<Message>> SearchDeadLettersMessages(string filter = null)
        {
            var messages = await GetAllDeadLettersMessagesAsync();

            if (string.IsNullOrWhiteSpace(filter)) return messages;

            return messages.AsQueryable().Where(filter);
        }

        public async Task<IEnumerable<Message>> GetAllDeadLettersMessagesAsync()
        {
            _logger?.LogDebug($"Sending message to {QueueName}.");
            var deadQueuePath = EntityNameHelper.FormatDeadLetterPath(QueueName);
            var deadQueueReceiver = new MessageReceiver(ConnectionString, deadQueuePath, ReceiveMode.PeekLock, null, 100);
            var result = new List<Message>();
            Message message = null;
            do
            {
                message = await deadQueueReceiver.PeekAsync();
                if (message != null)
                {
                    result.Add(message);
                }
            } while (message != null);

            return result;
        }
    }
}
