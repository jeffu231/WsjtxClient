using WsjtxClient.Models;

namespace WsjtxClient.Events
{
    public class WsjtxDecodeEventArgs:EventArgs
    {
        public WsjtxDecodeEventArgs(WsjtxDecode decode)
        {
            Decode = decode;
        }

        public WsjtxDecode Decode { get; }
    }
}