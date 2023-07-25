using WsjtxClient.Messages;
using WsjtxClient.Messages.In;

namespace WsjtxClient.Provider;

public interface IWsjtxClient
{
    public Task<bool> SendMessage(IWsjtxCommandMessage msg);
    
    public event EventHandler<WsjtxMessage>? MessageReceived;

    void Start(Listener config, CancellationToken token);

    void Stop();
}