using System.Threading.Tasks;

namespace Ming.Core.Messaging
{
    public interface ISimpleDeadletterQueueClient
    {
        Task DeleteAllDeadLettersAsync();
    }
}
