using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ming.Core.Messaging
{
    /// <summary>
    /// Send and receive messages for queue.
    /// </summary>
    public interface ISimpleQueueClient
    {
        /// <summary>
        /// send message to the queue.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="queueName"></param>
        /// <param name="simpleMessage"></param>
        /// <returns></returns>
        Task SendAsync(SimpleMessage simpleMessage);

        /// <summary>
        /// listen for messages coming from the queue.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="queueName"></param>
        /// <param name="onMessage"></param>
        /// <param name="onError"></param>
        void OnMessageHandler(Action<SimpleMessage> onMessage, Action<Exception, string> onError = null);
        
        //
        // Summary:
        //     Moves a message to the deadletter sub-queue.
        //
        // Parameters:
        //   lockToken:
        //     The lock token of the corresponding message to deadletter.
        //
        //   deadLetterReason:
        //     The reason for deadlettering the message.
        //
        //   deadLetterErrorDescription:
        //     The error description for deadlettering the message.
        //
        // Remarks:
        //     A lock token can be found in Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken,
        //     only when Microsoft.Azure.ServiceBus.QueueClient.ReceiveMode is set to Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock.
        //     In order to receive a message from the deadletter queue, you will need a new
        //     Microsoft.Azure.ServiceBus.Core.IMessageReceiver, with the corresponding path.
        //     You can use Microsoft.Azure.ServiceBus.EntityNameHelper.FormatDeadLetterPath(System.String)
        //     to help with this. This operation can only be performed on messages that were
        //     received by this receiver.
        Task DeadLetterAsync(string lockToken, string deadLetterReason, string deadLetterErrorDescription = null);
        //
        // Summary:
        //     Moves a message to the deadletter sub-queue.
        //
        // Parameters:
        //   lockToken:
        //     The lock token of the corresponding message to deadletter.
        //
        //   propertiesToModify:
        //     The properties of the message to modify while moving to sub-queue.
        //
        // Remarks:
        //     A lock token can be found in Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken,
        //     only when Microsoft.Azure.ServiceBus.QueueClient.ReceiveMode is set to Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock.
        //     In order to receive a message from the deadletter queue, you will need a new
        //     Microsoft.Azure.ServiceBus.Core.IMessageReceiver, with the corresponding path.
        //     You can use Microsoft.Azure.ServiceBus.EntityNameHelper.FormatDeadLetterPath(System.String)
        //     to help with this. This operation can only be performed on messages that were
        //     received by this receiver.
        Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null);        
    }
}
