using WsjtxClient.Messages.Out;

namespace WsjtxClient.Events;

public class WsjtxQsoLoggedEventArgs:EventArgs
{
    public WsjtxQsoLoggedEventArgs(QsoLoggedMessage message)
    {
        Message = message;
    }

    public QsoLoggedMessage Message { get; }
}