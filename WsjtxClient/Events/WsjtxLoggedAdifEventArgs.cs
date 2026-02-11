using WsjtxClient.Messages.Out;

namespace WsjtxClient.Events;

/// <summary>
/// Event args for Adif messages
/// </summary>
public class WsjtxLoggedAdifEventArgs
{
    public WsjtxLoggedAdifEventArgs(LoggedAdifMessage message)
    {
        Message = message;
    }

    public LoggedAdifMessage Message { get; }
}