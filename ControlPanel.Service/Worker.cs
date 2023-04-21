using ControlPanel.Core.Helpers;

namespace ControlPanel
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SerialHelper _serialHelper;
        private readonly SshHelper _sshHelper;

        private readonly VolumeMonitor _volumeMonitor;
        private readonly SshMonitor _sshMonitor;
        protected SerialMonitor _serialMonitor;

        private const string _appName = "Youtube Music Desktop App";
        private bool _masterVolume = true;

        private VolumeListener _state;

        public VolumeListener State
        {
            get
            {
                return _state;
            }
            set
            {
                // When changing the volume source we want to 'listen' to, the SSH Monitor uses polling.
                // So, we need to enable/disable the timer based on the state
                _sshMonitor.timer.Enabled = value == VolumeListener.RemoteSystem;
                _state = value;
            }
        }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _logger.LogTrace("Worker started at: {time}", DateTimeOffset.Now);

            //TODO: Add support for finding the serial port and identifying the correct device

            _serialHelper = new("COM3", 115200);

            // Not ideal to keep this password as default, but SSH is configured to turn off on the iPhone
            // once disconnected from the wifi network.
            _sshHelper = new("192.168.1.64", 2222, "mobile", "alpine");

            _sshMonitor = new(_sshHelper);
            _sshMonitor.VolumeChanged += SshMonitor_VolumeChanged;

            _volumeMonitor = new();
            _volumeMonitor.MuteChanged += VolumeMonitor_MuteChanged;
            _volumeMonitor.VolumeChanged += VolumeMonitor_VolumeChanged;

            State = VolumeListener.LocalSystem;
            
            // Control App volume by default
            _masterVolume= false;
            _volumeMonitor.SetApp(_appName);

            _logger.LogInformation("controlling {source} volume", _masterVolume ? "master" : _appName);
        }

        private void VolumeMonitor_MuteChanged(object? sender, VolumeChangedEventArgs e)
        {
            _logger.LogTrace("{source} muted", _masterVolume ? "master" : _appName);
            if (State == VolumeListener.LocalSystem)
            {
                _serialHelper.SetMute(e.Mute);
            }
        }

        private void VolumeMonitor_VolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            _logger.LogTrace("{source} volume changed to: {volume:00}", _masterVolume ? "master" : _appName, e.Volume);
            if (State == VolumeListener.LocalSystem && !_serialMonitor.Fader.Touched)
            {
                _serialHelper.SetLevel((int)e.Volume);
            }
        }

        private void SerialMonitor_LevelChanged(object? sender, FaderLevelChangedEventArgs e)
        {
            if (e.Fader.Touched && State == VolumeListener.LocalSystem)
            {
                _logger.LogTrace("setting {source} volume to: {level:00}", _masterVolume ? "master" : _appName, e.Fader.Level);
                _volumeMonitor.SetVolume(e.Fader.Level);
            }
            else if (e.Fader.Touched && State == VolumeListener.RemoteSystem)
            {
                _logger.LogTrace("setting remote volume to {level:00}", e.Fader.Level);
                _sshHelper.SetVolume(e.Fader.Level);
            }
        }

        private void SerialMonitor_DoubleClicked(object? sender, FaderLevelChangedEventArgs e)
        {
            _logger.LogInformation("slider double-clicked");
            if (State == VolumeListener.RemoteSystem)
            {
                State = VolumeListener.LocalSystem;
                _logger.LogInformation("controlling local system - master volume");
                _masterVolume = true;
                _volumeMonitor.SetApp();
            }
            else if (State == VolumeListener.LocalSystem)
            {
                if (_masterVolume)
                {   
                    _masterVolume = false;
                    _volumeMonitor.SetApp(_appName);
                }
                else
                {
                    _masterVolume = true;
                    _volumeMonitor.SetApp();
                }
                _logger.LogInformation("controlling {source} volume", _masterVolume ? "master" : _appName);
            }
        }

        private void SerialMonitor_TripleClicked(object? sender, FaderLevelChangedEventArgs e)
        {
            _logger.LogTrace("slider triple-clicked");
            if (State == VolumeListener.LocalSystem)
            {
                _logger.LogInformation("controlling remote system");
                State = VolumeListener.RemoteSystem;
            }
            else if (State == VolumeListener.RemoteSystem)
            {
                _logger.LogInformation("controlling local system");
                State = VolumeListener.LocalSystem;
            }
        }

        private void SerialMonitor_HeldDown(object? sender, FaderLevelChangedEventArgs e)
        {
            _logger.LogInformation("slider held down {holdCount} times", e.Fader.HoldCount);
        }

        private void SshMonitor_VolumeChanged(object? sender, PhoneChangedEventArgs e)
        {
            _logger.LogTrace("remote volume changed to: {volume:00}", e.Phone.Volume);
            if (State == VolumeListener.RemoteSystem && e.Phone.Volume >= 0 && e.Phone.Volume <= 100 && !_serialMonitor.Fader.Touched)
            {
                _serialHelper.SetLevel((int)e.Phone.Volume);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _serialMonitor = new(_serialHelper, stoppingToken);
            _serialMonitor.LevelChanged += SerialMonitor_LevelChanged;
            _serialMonitor.DoubleClicked += SerialMonitor_DoubleClicked;
            _serialMonitor.TripleClicked += SerialMonitor_TripleClicked;

            // HeldDown is experiencing issues from the arduino, so temporarily disabled
            //_serialMonitor.HeldDown += SerialMonitor_HeldDown;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    public enum VolumeListener
    {
        LocalSystem, // Local hardware
        RemoteSystem // Phone
    }
}