using ControlPanel.Core.Helpers;
using ControlPanel.Core.Providers;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ControlPanel.Mqtt
{
    public class Worker : BackgroundService
    {
        private const int MONITOR_DELAY = 200;
        private const int SOURCE_DELAY = 5000;

        private readonly ILogger<Worker> _logger;
        private readonly IVolumeProvider _volumeProvider;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        private readonly Dictionary<string, VolumeInfo> _volumes;
        private bool isRunning = true;

        public Worker(
            ILogger<Worker> logger,
            IVolumeProvider volumeProvider)
        {
            _logger = logger;
            _volumeProvider = volumeProvider;

            _mqttClient = (new MqttFactory()).CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceived;

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("homeassistant.local")
                .WithCredentials("mqtt", "broker")
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithCleanSession(true)
                .Build();

            _mqttClient.ConnectAsync(_mqttClientOptions).GetAwaiter().GetResult();

            _volumes = new();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            _logger.LogInformation("Monitoring system volume.");
            VolumeInfo vInfo = new(_volumeProvider);
            _volumes.Add("SystemVolume", vInfo);

            vInfo.VolumeChanged += VolumeInfo_VolumeChanged;
            vInfo.MutedChanged += VolumeInfo_MutedChanged;

            _mqttClient.SubscribeAsync("esp32iotsensor/ControlPanel/in", MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken).GetAwaiter().GetResult();

            while (isRunning && !stoppingToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    try
                    {
                        _mqttClient.ConnectAsync(_mqttClientOptions, stoppingToken).GetAwaiter().GetResult();
                    }
                    catch { }
                }

                var apps = CoreAudioHelper.GetAudioApps();
                foreach (var app in apps)
                {
                    string appName = app.ProcessName.Replace(" ", "");
                    if (!_volumes.Select(pair => pair.Key).Contains(appName))
                    {
                        _logger.LogInformation("Monitoring {appName}.", appName);
                        vInfo = new(_volumeProvider, appName);
                        _volumes.Add(appName, vInfo);

                        vInfo.VolumeChanged += VolumeInfo_VolumeChanged;
                        vInfo.MutedChanged += VolumeInfo_MutedChanged;
                    }
                }
                foreach (var volume in _volumes.Where(v => v.Key != "SystemVolume"))
                {
                    // If a previously monitored application is no longer available, end that thread
                    if (!apps.Select(a => a.ProcessName.Replace(" ", "")).Contains(volume.Key))
                    {
                        _logger.LogInformation("No longer monitoring {appName}.", volume.Key);
                        volume.Value.Join();
                        _volumes.Remove(volume.Key);
                    }
                }
                Thread.Sleep(SOURCE_DELAY);
                await Task.Delay(SOURCE_DELAY, stoppingToken);
            }

            isRunning = false;
            _mqttClient?.DisconnectAsync(cancellationToken: stoppingToken).GetAwaiter().GetResult();
            foreach (var volumeThread in _volumes)
            {
                volumeThread.Value.Join();
            }
        }

        private void VolumeInfo_VolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            var vInfo = (VolumeInfo)sender;
            if (e.Mute)
            {
                _mqttClient.ApplicationMessageReceivedAsync -= ApplicationMessageReceived;
            }
            SendLevelMessage(vInfo.Topic, e.Mute ? 0 : e.Volume);
            if (e.Mute)
            {
                Thread.Sleep(100);
                _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceived;
            }
            _logger.LogTrace("{source} volume changed to: {volume:00}", vInfo.Topic, e.Volume);
        }

        private void VolumeInfo_MutedChanged(object? sender, VolumeChangedEventArgs e)
        {
            var vInfo = (VolumeInfo)sender;
            if (e.Mute)
            {
                _mqttClient.ApplicationMessageReceivedAsync -= ApplicationMessageReceived;
            }
            SendLevelMessage(vInfo.Topic, e.Mute ? 0 : e.Volume);
            if (e.Mute)
            {
                Thread.Sleep(100);
                _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceived;
            }
            _logger.LogTrace("{source} muted", vInfo.Topic);
        }

        private void SendLevelMessage(string topicName, float level)
        {
            string payload = string.Empty;
            if (topicName == "system")
            {
                payload = $"{{\"slide0\": {(int)Math.Round(level)}}}";
            }
            else if (topicName == "YouTubeMusicDesktopApp")
            {
                payload = $"{{\"slide1\": {(int)Math.Round(level)}}}";
            }
            MqttApplicationMessage mqttApplicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic($"esp32iotsensor/ControlPanel/out")
                            .WithPayload(payload).Build();

            _mqttClient.PublishAsync(mqttApplicationMessage, CancellationToken.None).Wait();
        }

        private Task ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs arg)
        {
            MqttApplicationMessage message = arg.ApplicationMessage;

            string payload = message.ConvertPayloadToString();
            _logger.LogTrace("received {payload}", payload);

            try
            {
                if (payload.Contains("slide"))
                {
                    int level = int.Parse(payload[(payload.IndexOf(":") + 1)..^1].Trim());
                    if (payload.Contains("slide0"))
                    {
                        _volumes["SystemVolume"].SetVolume(level);
                    }
                    else if (payload.Contains("slide1"))
                    {
                        _volumes["YouTubeMusicDesktopApp"].SetVolume(level);
                    }
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        private class VolumeInfo
        {
            private readonly List<Thread> _setThreads = new();
            private readonly IVolumeProvider _volumeProvider;
            private readonly Thread? _thread;
            private long _millis = DateTime.Now.Ticks;
            private bool _isRunning = false;
            private bool _muted = false;
            private int _volume = -1;

            public readonly string Topic;

            public int Volume
            {
                get
                {
                    return _volume;
                }
                set
                {
                    if (_volume != value)
                    {
                        _volume = value;
                        On_VolumeChanged();
                    }
                }
            }
            public bool Muted
            {
                get
                {
                    return _muted;
                }
                set
                {
                    if (_muted != value)
                    {
                        _muted = value;
                        On_MuteChanged();
                    }
                }
            }

            public void On_VolumeChanged()
            {
                var handler = VolumeChanged;
                if (handler != null)
                {
                    var args = new VolumeChangedEventArgs() { Volume = _volume, Mute = _muted };
                    handler(this, args);
                }
            }

            public void On_MuteChanged()
            {
                var handler = MutedChanged;
                if (handler != null)
                {
                    var args = new VolumeChangedEventArgs() { Volume = _volume, Mute = _muted };
                    handler(this, args);
                }
            }

            public VolumeInfo(IVolumeProvider volumeProvider, string appName = "system")
            {
                Topic = appName;
                _volumeProvider = volumeProvider;

                _isRunning = true;
                _thread = new Thread(CheckVolume);
                _thread.Start();
            }

            public void SetVolume(int level)
            {
                if (_volume == level)
                {
                    return;
                }

                _millis = DateTime.Now.Ticks;
                if (Topic == "system")
                {
                    _volumeProvider.SetSystemVolumeLevel(level);
                }
                else
                {
                    _volumeProvider.SetApplicationVolumeLevel(Topic, level);
                }

                _volume = level;
                if (level > 0 && _muted)
                {
                    SetMute(false);
                }
            }

            public void SetMute(bool mute)
            {
                if (Topic == "system")
                {
                    _volumeProvider.SetSystemVolumeMute(mute);
                }
                else
                {
                    _volumeProvider.SetApplicationVolumeMute(Topic, mute);
                }
                _muted = mute;
            }

            private void CheckVolume()
            {
                while (_isRunning)
                {
                    // If we've just set the volume within our delay window, 'pause' checking temporarily
                    if(_millis > DateTime.Now.Ticks - MONITOR_DELAY * 2)
                    {
                        continue;
                    }
                    try
                    {
                        if (Topic == "system")
                        {
                            Volume = _volumeProvider.GetSystemVolumeLevel();
                            Muted = _volumeProvider.GetSystemVolumeMute();
                        }
                        else
                        {
                            Volume = _volumeProvider.GetApplicationVolumeLevel(Topic);
                            Muted = _volumeProvider.GetApplicationVolumeMute(Topic);
                        }

                        Thread.Sleep(MONITOR_DELAY);
                    }
                    catch { }
                }
            }


            public void Join()
            {
                _isRunning = false;
                foreach (Thread t in _setThreads)
                {
                    t.Join();
                }

                _thread?.Join();
            }

            public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;
            public event EventHandler<VolumeChangedEventArgs>? MutedChanged;
        }

        private class VolumeChangedEventArgs : EventArgs
        {
            public float Volume { get; set; }
            public bool Mute { get; set; }
        }
    }
}
