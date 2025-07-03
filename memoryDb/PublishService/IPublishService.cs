using System.Net.Sockets;

namespace RedisServer.Publish.Service
{
    public interface IPublishService
    {
        void AddSubscription(string channel, Socket socket);
        void Unsubscribe(string channel, Socket client);
        int Publish(string channel, string message);
        int SubscriptionCount(Socket socket);
        void AddPatternSubscription(string globPattern, Socket socket);
        void RemovePatternSubscription(string globPattern, Socket socket);
    }
}