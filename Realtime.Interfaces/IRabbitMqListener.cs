using System.Threading;

namespace Realtime.Tester.RabbitMq
{
    public interface IRabbitMqListener
    {
        string GetMessage(string hostname, CancellationToken token, AutoResetEvent messageReceivedWaitHandle, AutoResetEvent channelCreatedWaitHandle);
        CancellationTokenSource StartListening(string mirthhostname);
    }
}