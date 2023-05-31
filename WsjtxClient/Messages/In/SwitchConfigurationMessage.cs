namespace WsjtxClient.Messages.In
{
    /*
     * SwitchConfiguration  In 14                     quint32
     *                         Id (unique key)        utf8
     *                         Configuration Name     utf8
     *
     *      The server  may send  this message at  any time.   The message
     *      specifies the name of the  configuration to switch to. The new
     *      configuration must exist.
     */

    public class SwitchConfigurationMessage :WsjtxMessage, IWsjtxCommandMessage
    {
        public string ConfigurationName { get; set; } = string.Empty;
        public byte[] GetBytes()  {
            
            return Qt.Concat(
                MAGIC_NUMBER,
                SCHEMA_VERSION,
                Qt.Encode((uint)MessageType.SWITCH_CONFIG_MESSAGE),
                Qt.Encode(Id),
                Qt.Encode(ConfigurationName));
        }
    }
}
