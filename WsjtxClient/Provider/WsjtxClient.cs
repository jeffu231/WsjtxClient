using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using WsjtxClient.Messages;
using WsjtxClient.Messages.Both;
using WsjtxClient.Messages.In;

namespace WsjtxClient.Provider
{
    /// <summary>
    /// WSJTX Client class based on the example from M0LTE.WsjtxUdpLib
    /// </summary>
    public sealed class WsjtxClient : IWsjtxClient, IDisposable
    {
        private UdpClient? _udpClient;
        private readonly ConcurrentDictionary<string, IPEndPoint> _endPoints;
        private readonly ILogger<WsjtxClient> _logger;
        private bool _running = false;
        private CancellationTokenSource _token;
        private IPAddress? _ipAddress;
        private bool _isMulticast = false;
        
        public WsjtxClient(ILogger<WsjtxClient> logger)
        {
            _logger = logger;
            _endPoints = new ConcurrentDictionary<string, IPEndPoint>();
            _token = new CancellationTokenSource();
        }

        public void Start(Listener configuration, CancellationToken token)
        {
            _token = CancellationTokenSource.CreateLinkedTokenSource(token);
            
            try
            {
                _ipAddress = IPAddress.Parse(configuration.Ip);
                var port = configuration.Port;
                _isMulticast = configuration.Multicast;
                if (_isMulticast)
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                    _udpClient.JoinMulticastGroup(_ipAddress);
                }
                else
                {
                    _udpClient = new UdpClient(new IPEndPoint(_ipAddress, port));
                }
                
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
            }
            catch (Exception e)
            {
                _logger.LogCritical("Invalid IP Address {Message}", e.Message);
                throw;
            }

            _ = Task.Run(UdpLoop, token);
        }

        public void Stop()
        {
            _running = false;
            if (_isMulticast && _ipAddress != null)
            {
                _udpClient?.DropMulticastGroup(_ipAddress);
            }
            
            _token.Cancel();
            _udpClient?.Close();
            _logger.LogInformation("Wsjtx Client stopped");
        }
        
        private async Task UdpLoop()
        {
            if (_udpClient == null) throw new Exception("UdpClient is null!");
            
            _running = true;
            while (_running)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_token.Token);
                    byte[] datagram = result.Buffer;
                    var msg = WsjtxMessage.Parse(datagram);
                    if (msg == null)
                    {
                        _logger.LogWarning("Received null message");
                        continue;
                    }
                    if (msg is CloseMessage cm)
                    {
                        _endPoints.TryRemove(msg.Id, out _);
                    }
                    else 
                    {
                        _endPoints[msg.Id] = result.RemoteEndPoint;
                    }

                    OnMessageReceived(msg);

                    _logger.LogTrace("Message for {MsgId} received from {From}",
                        msg.Id, result.RemoteEndPoint);
                }
                catch (ParseFailureException ex)
                {
                    _logger.LogError(ex,"Parse failure for {ExMessageType}", ex.MessageType);
                }
                catch (OperationCanceledException oc)
                {
                    _logger.LogTrace(oc, "Udp receive cancelled");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception on client receive");
                }
            }
        }

        public async Task<bool> SendMessage(IWsjtxCommandMessage msg)
        {
            if (_udpClient == null) return false;
            if (_endPoints.TryGetValue(msg.Id, out var endPoint))
            {
                var bytesToSend = msg.GetBytes();
                try
                {
                    await _udpClient.SendAsync(bytesToSend, bytesToSend.Length, endPoint);
                    _logger.LogTrace("Sent wsjtx msg bytes {Length} to {S}", bytesToSend.Length, endPoint.ToString());
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError("Send wsjtx msg failed {EMessage}", e.Message);
                }
            }

            return false;
        }
        
        public void Dispose()
        {
            _udpClient?.Dispose();
        }

        public event EventHandler<WsjtxMessage>? MessageReceived;

        private void OnMessageReceived(WsjtxMessage msg)
        {
            MessageReceived?.Invoke(this, msg);
        }
    }
}