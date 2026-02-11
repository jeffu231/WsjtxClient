using WsjtxClient.Messages.Out;

namespace WsjtxClient.Events;

public class WsjtxLoggedAdifEventArgs
{
    public WsjtxLoggedAdifEventArgs(LoggedAdifMessage message)
    {
        Message = message;
    }

    public LoggedAdifMessage Message { get; }
}