using WsjtxClient.Models;

namespace WsjtxClient.Events
{
    public class WsjtxStatusEventArgs
    {
        public WsjtxStatusEventArgs(WsjtxStatus status)
        {
            Status = status;
        }

        public WsjtxStatus Status { get; }
    }
}