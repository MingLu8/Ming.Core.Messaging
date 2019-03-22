using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ming.Core.Messaging
{
    public class DefaultQueueClient : ISimpleQueueClient
    {      
        private readonly ILoggerFactory _loggerFactory;

        private ILogger _logger;
        private IQueueClient _receiverQueueClient;

        public virtual string ConnectionString { get; set; }
        public virtual string QueueName { get; }
        public virtual string MessageVersion { get; }

        protected virtual ILogger Logger => _logger ?? (_logger = _loggerFactory?.CreateLogger<DefaultQueueClient>());

        protected virtual IQueueClient ReceriverQueueClient => _receiverQueueClient ?? (_receiverQueueClient = new QueueClient(ConnectionString, QueueName));
      
        public DefaultQueueClient(string connectionString, string queueName, string messageVersion=null, ILoggerFactory loggerFactory=null)
        {
            ConnectionString = connectionString;
            QueueName = queueName;
            MessageVersion = messageVersion;
            _loggerFactory = loggerFactory;
        }

        public virtual async Task SendAsync(SimpleMessage simpleMessage)
        {
            _logger?.LogDebug($"Sending message to {QueueName}.");
            var senderQueueClient = new QueueClient(ConnectionString, QueueName);

            var json = string.Empty;
            try
            {
                var message = new Message(Encoding.UTF8.GetBytes(simpleMessage.Body))
                {
                    Label = simpleMessage.Label,
                    CorrelationId = simpleMessage.CorrelationId,
                };
                if (!string.IsNullOrWhiteSpace(simpleMessage.MessageId))
                    message.MessageId = simpleMessage.MessageId;

                if (simpleMessage.TimeToLive.HasValue)
                    message.TimeToLive = simpleMessage.TimeToLive.Value;

                foreach (var key in simpleMessage.UserProperties.Keys)
                {
                    message.UserProperties.Add(key, simpleMessage.UserProperties[key]);
                }

                await senderQueueClient.SendAsync(message);
                _logger?.LogDebug($"Sending message to {QueueName} succeeded.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Sending message  to {QueueName} failed, message json: {json}. Error: {ex.Message}.");
                throw;
            }
            finally
            {
                await senderQueueClient.CloseAsync();
            }
        }

        public virtual void OnMessageHandler(Action<SimpleMessage> onMessage, Action<Exception, string> onError = null)
        {
            ReceriverQueueClient.RegisterMessageHandler(async (message, token) =>
            {
                try
                {
                    if(IsCompatibleMessage(message))
                    { 
                        await ReceriverQueueClient.AbandonAsync(message.SystemProperties.LockToken);
                        return;
                    }

                    var body = Encoding.UTF8.GetString(message.Body);
                    onMessage(new SimpleMessage(body, message.CorrelationId, message.MessageId, message.SystemProperties.LockToken));
                    // Complete the message so that it is not received again.
                    // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
                    await ReceriverQueueClient.CompleteAsync(message.SystemProperties.LockToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Process message failed with error: {ex.Message}");
                }
            }
            , CreateHandlerOptions(onError));
        }

        public virtual Task DeadLetterAsync(string lockToken, string deadLetterReason, string deadLetterErrorDescription = null)
        {
            return _receiverQueueClient.DeadLetterAsync(lockToken, deadLetterReason, deadLetterErrorDescription);
        }

        public virtual Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            return _receiverQueueClient.DeadLetterAsync(lockToken, propertiesToModify);
        }

        protected virtual MessageHandlerOptions CreateHandlerOptions(Action<Exception, string> onError)
        {

            return new MessageHandlerOptions(async (exceptionArg) => await Task.Run(CreateExceptionReceivedHandler(onError, exceptionArg)))
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };
        }

        private Action CreateExceptionReceivedHandler(Action<Exception, string> onError, ExceptionReceivedEventArgs exceptionArg)
        {
            return () =>
            {
                _logger?.LogError(exceptionArg.Exception.Message);
                onError?.Invoke(exceptionArg.Exception, exceptionArg.ExceptionReceivedContext.Endpoint);
            };
        }

        private bool IsCompatibleMessage(Message message)
        {
            var checkVersion = (message.UserProperties["CheckVersion"] != null) && (bool)message.UserProperties["CheckVersion"];
            return !(checkVersion && (string)message.UserProperties["MessageVersion"] == MessageVersion);
        }
    }
}
