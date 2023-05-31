﻿namespace WsjtxClient.Messages.In
{
    /*
     * Halt Tx       In        8
     *                         Id (unique key)        utf8
     *                         Auto Tx Only           bool
     *
     *      The server may stop a client from transmitting messages either
     *      immediately or at  the end of the  current transmission period
     *      using this message.
     */

    public class HaltTxMessage : IWsjtxCommandMessage
    {
        public string Id { get; set; }
        public byte[] GetBytes() => throw new NotImplementedException();
    }
}
