namespace WsjtxClient.Messages.In
{
    public interface IWsjtxCommandMessage: IWsjtxMessage
    {
        byte[] GetBytes();
    }
}
