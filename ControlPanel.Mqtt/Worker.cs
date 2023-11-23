using ControlPanel.Core.Factories;
using ControlPanel.Core.Helpers;
using ControlPanel.Core.Providers;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ControlPanel.Mqtt
{
    public class Worker : BackgroundService
    {

        private const int SOURCE_DELAY = 5000;

        private readonly ILogger<Worker> _logger;
        private readonly IVolumeProvider _volumeProvider;
        private readonly IAudioHelper _audioHelper;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        private readonly Dictionary<string, VolumeInfo> _volumes;
        private bool isRunning = true;

        public Worker(
            ILogger<Worker> logger,
            IVolumeProviderFactory volumeProviderFactory,
            IAudioHelperFactory audioHelperFactory)
        {
            _logger = logger;
            _volumeProvider = volumeProviderFactory.GetVolumeProvider();
            _audioHelper = audioHelperFactory.GetAudioHelper();

            // Used mycurvefit.com to visualize and customize the curve
            _audioHelper.AddCurve(0, 0);
            _audioHelper.AddCurve(60, 36);
            _audioHelper.AddCurve(100, 100);

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
                        _mqttClient.SubscribeAsync("esp32iotsensor/ControlPanel/in", MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken).GetAwaiter().GetResult();
                    }
                    catch { }
                }

                var apps = _audioHelper.GetAudioApps();
                foreach (var app in apps)
                {
                    string appName = app;
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
                    if (!apps.Contains(volume.Key))
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
    }
}
