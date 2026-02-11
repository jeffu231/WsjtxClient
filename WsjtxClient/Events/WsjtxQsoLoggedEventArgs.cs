using WsjtxClient.Messages.Out;

namespace WsjtxClient.Events;

/// <summary>
/// Event args for QSO Logged events
/// </summary>
public class WsjtxQsoLoggedEventArgs:EventArgs
{
    public WsjtxQsoLoggedEventArgs(QsoLoggedMessage message)
    {
        Message = message;
    }

    public QsoLoggedMessage Message { get; }
}