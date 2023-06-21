using ControlPanel.Core.Helpers;
using ControlPanel.Core.Providers;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Collections;
using System.Linq;

namespace ControlPanel.Mqtt
{
    public class Worker : BackgroundService
    {
        private const int MONITOR_DELAY = 500;
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
            _volumes.Add("SystemVolume", new VolumeInfo(new Thread(CheckSystemVolume)));
            _volumes["SystemVolume"].VolumeChanged += VolumeInfo_VolumeChanged;
            _volumes["SystemVolume"].MutedChanged += VolumeInfo_MutedChanged;

            _mqttClient.SubscribeAsync("esp32iotsensor/ControlPanel/in", MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken).GetAwaiter().GetResult();

            while (!stoppingToken.IsCancellationRequested)
            {
                var apps = CoreAudioHelper.GetAudioApps();
                foreach (var app in apps)
                {
                    string appName = app.ProcessName.Replace(" ", "");
                    if (!_volumes.Select(pair => pair.Key).Contains(appName))
                    {
                        _logger.LogInformation("Monitoring {appName}.", appName);
                        _volumes.Add(appName, new VolumeInfo(appName, new Thread(() => CheckAppVolume(appName))));

                        _volumes[appName].VolumeChanged += VolumeInfo_VolumeChanged;
                        _volumes[appName].MutedChanged += VolumeInfo_MutedChanged;
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
            SendLevelMessage(vInfo.Topic, e.Volume);
            _logger.LogTrace("{source} volume changed to: {volume:00}", vInfo.Topic, e.Volume);
        }

        private void VolumeInfo_MutedChanged(object? sender, VolumeChangedEventArgs e)
        {
            var vInfo = (VolumeInfo)sender;
            //SendMuteMessage(vInfo.Topic, e.Mute);
            _logger.LogTrace("{source} muted", vInfo.Topic);
        }

        private void CheckSystemVolume()
        {
            VolumeInfo vInfo = _volumes["SystemVolume"];
            try
            {
                while (isRunning)
                {
                    vInfo.Volume = _volumeProvider.GetSystemVolumeLevel();
                    vInfo.Muted = _volumeProvider.GetSystemVolumeMute();

                    Thread.Sleep(MONITOR_DELAY);
                }
            }
            catch { }
        }

        private void CheckAppVolume(string appName)
        {
            VolumeInfo vInfo = _volumes[appName];
            try
            {
                while (isRunning)
                {
                    vInfo.Volume = _volumeProvider.GetApplicationVolumeLevel(appName);
                    vInfo.Muted = _volumeProvider.GetApplicationVolumeMute(appName);


                    Thread.Sleep(MONITOR_DELAY);
                }
            }
            catch { }
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

            if (payload.Contains("slide"))
            {
                int level = int.Parse(payload[(payload.IndexOf(":") + 1)..^1].Trim());
                if (payload.Contains("slide0"))
                {
                    VolumeInfo vInfo = _volumes["SystemVolume"];
                    vInfo.VolumeChanged -= VolumeInfo_VolumeChanged;
                    vInfo.MutedChanged -= VolumeInfo_MutedChanged;
                    _volumeProvider.SetSystemVolumeLevel(level);
                    vInfo.Volume = _volumeProvider.GetSystemVolumeLevel();
                    vInfo.VolumeChanged += VolumeInfo_VolumeChanged;
                    vInfo.MutedChanged += VolumeInfo_MutedChanged;
                }
                else if (payload.Contains("slide1"))
                {
                    // Using threads here to improve response times
                    (new Thread(() =>
                    {
                        _volumeProvider.SetApplicationVolumeLevel("YouTubeMusicDesktopApp", level);
                    })).Start();
                }
            }

            return Task.CompletedTask;
        }

        private class VolumeInfo
        {
            private readonly Thread _thread;
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

            public VolumeInfo(Thread thread)
            {
                Topic = "system";
                _thread = thread;

                _thread.Start();
            }

            public VolumeInfo(string appName, Thread thread)
            {
                Topic = appName;
                _thread = thread;

                _thread.Start();
            }

            public void Join()
            {
                _thread.Join();
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
