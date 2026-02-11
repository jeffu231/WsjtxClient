using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WsjtxClient.Events;
using WsjtxClient.Messages;
using WsjtxClient.Messages.Both;
using WsjtxClient.Messages.In;
using WsjtxClient.Messages.Out;
using WsjtxClient.Models;

namespace WsjtxClient.Provider
{
    public class WsjtxDataProvider: BackgroundService, IWsjtxDataProvider
    {
        private readonly ILogger<WsjtxDataProvider> _logger;
        private readonly ConcurrentDictionary<string, WsjtxStatus> _wsjtxStatus;
        private readonly Dictionary<string, DateTime> _activeInstances;
        private readonly Listener _config;

        private IWsjtxClient _wsjtxClient;

        public WsjtxDataProvider(ILogger<WsjtxDataProvider> logger, IWsjtxClient wsjtxClient, Listener config)
        {
            _logger = logger;
            _wsjtxClient = wsjtxClient;
            _activeInstances = new Dictionary<string, DateTime>();
            _wsjtxStatus = new ConcurrentDictionary<string, WsjtxStatus>();
            _config = config;
        }
        
        public WsjtxDataProvider(ILogger<WsjtxDataProvider> logger, IWsjtxClient wsjtxClient, IConfiguration configuration):
            this(logger, wsjtxClient, new Listener
            {
                Ip = configuration["Wsjtx:Listener:Ip"] ?? "127.0.0.1",
                Port = configuration.GetValue<int>("Wsjtx:Listener:Port"),
                Multicast = configuration.GetValue<bool>("Wsjtx:Listener:Multicast")
            })
        {
             
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _wsjtxClient.MessageReceived += WsjtxClientOnMessageReceived;
            _wsjtxClient.Start(_config, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                foreach (var activeInstance in _activeInstances.ToList())
                {
                    if (now - activeInstance.Value > TimeSpan.FromMinutes(2))
                    {
                        _logger.LogInformation("Removing {Id} not heard last 2 minutes", activeInstance.Key);
                        _activeInstances.Remove(activeInstance.Key);
                        _wsjtxStatus.Remove(activeInstance.Key, out _);
                    }
                }
                await Task.Delay(30000, stoppingToken);
            }
            
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WsjtxDataProvider service stopping");
            _wsjtxClient.Stop();
            var t = base.StopAsync(cancellationToken);
            _logger.LogInformation("WsjtxDataProvider service stopped");
            return t;
        }

        private void WsjtxClientOnMessageReceived(object? sender, WsjtxMessage msg)
        {
            if (msg is DecodeMessage dm)
            {
                _logger.LogTrace("Decode for {Id}", dm.Id);
                ParseDecodeMessage(dm);
            }
            else if (msg is StatusMessage sm)
            {
                _logger.LogTrace("Status for {Id}", sm.Id);
                ParseStatusMessage(sm);
            }
            else if (msg is HeartbeatMessage hm)
            {
                _logger.LogTrace("Heartbeat for {Id}", hm.Id);
                ParseHeartbeatMessage(hm);
            }
            else if (msg is QsoLoggedMessage qsoLoggedMessage)
            {
                _logger.LogTrace("Qso Logged for {Id}", qsoLoggedMessage.Id);
                OnQsoLoggedReceived(qsoLoggedMessage);
            }
            else if (msg is LoggedAdifMessage loggedAdifMessage)
            {
                _logger.LogTrace("Adif Logged for {Id}", loggedAdifMessage.Id);
                OnAdifLoggedReceived(loggedAdifMessage);
            }
        }

        private void ParseStatusMessage(StatusMessage msg)
        {
            var status = WsjtxStatus.DecodeMessage(msg);
            if (_wsjtxStatus.ContainsKey(status.Id))
            {
                _wsjtxStatus[status.Id] = status;
            }
            else
            {
                _wsjtxStatus.TryAdd(status.Id, status);
            }
            
            OnStatusReceived(status);
        }

        private void ParseDecodeMessage(DecodeMessage msg)
        {
            var decode = WsjtxDecode.DecodeMessage(msg);
            OnDecodeReceived(decode);
        }

        private void ParseHeartbeatMessage(HeartbeatMessage msg)
        {
            _logger.LogInformation("{Beat}", msg.ToString() );
            if (_activeInstances.ContainsKey(msg.Id))
            {
                _activeInstances[msg.Id] = DateTime.Now;
            }
            else
            {
                _activeInstances.TryAdd(msg.Id, DateTime.Now);
            }
        }

        public Guid Id { get; } = Guid.NewGuid();
        
        public List<string> Instances => _wsjtxStatus.Keys.ToList();

        /// <summary>
        /// Gets the most recent status information for the WSJT-X instance the provider is aware of
        /// with the id name specified.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public WsjtxStatus? Status(string id)
        {
            return _wsjtxStatus.GetValueOrDefault(id);
        }

        /// <summary>
        /// The IP that this provider is listening on
        /// </summary>
        public string ListenerIp => _config.Ip;

        /// <summary>
        /// The port this provider is listening on
        /// </summary>
        public int ListenerPort => _config.Port;

        /// <summary>
        /// Send a broadcast WSJT-X message to the clients listening on this providers <see cref="ListenerIp"/> and <see cref="ListenerPort"/>
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> SendMessage(IWsjtxCommandMessage msg)
        {
            return await _wsjtxClient.SendMessage(msg);
        }
        
        /// <summary>
        /// Event handler for QSO Logged Messages
        /// </summary>
        public event EventHandler<WsjtxQsoLoggedEventArgs>? QsoLogReceived;
        
        /// <summary>
        /// Event handler for Logged Adif Messages
        /// </summary>
        public event EventHandler<WsjtxLoggedAdifEventArgs>? LoggedAdifReceived;

        public event EventHandler<WsjtxDecodeEventArgs>? DecodeReceived;
        
        public event EventHandler<WsjtxStatusEventArgs>? StatusReceived;
        
        private void OnAdifLoggedReceived(LoggedAdifMessage msg)
        {
            LoggedAdifReceived?.Invoke(this,new WsjtxLoggedAdifEventArgs(msg));
        }
        
        private void OnQsoLoggedReceived(QsoLoggedMessage msg)
        {
            QsoLogReceived?.Invoke(this,new WsjtxQsoLoggedEventArgs(msg));
        }

        private void OnDecodeReceived(WsjtxDecode decode)
        {
            DecodeReceived?.Invoke(this,new WsjtxDecodeEventArgs(decode));
        }
        
        private void OnStatusReceived(WsjtxStatus status)
        {
            StatusReceived?.Invoke(this,new WsjtxStatusEventArgs(status));
        }
        
    }
}