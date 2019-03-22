using System;
using System.Collections.Generic;

namespace Ming.Core.Messaging
{
    public class SimpleMessage
    {
        public virtual IDictionary<string, object> UserProperties { get; } = new Dictionary<string, object>();
        
        public virtual string LockToken { get; }
        public virtual string Label { get; set; }
       
        public virtual TimeSpan? TimeToLive { get; set; }

        public virtual string MessageId { get; }
        public virtual string CorrelationId { get; }

        public virtual string Body { get; }

        public SimpleMessage(string jsonBody, string correlationId = null, string messageId = null, string lockToken = null)
        {
            Body = jsonBody;
            CorrelationId = correlationId;
            MessageId = messageId;
            LockToken = lockToken;
        }
    }
}
