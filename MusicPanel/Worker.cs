using MusicPanel.Core.Helpers;

namespace MusicPanel
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SerialHelper _serialHelper;
        private readonly SshHelper _sshHelper;

        private readonly VolumeMonitor _volumeMonitor;
        private readonly SshMonitor _sshMonitor;
        private SerialMonitor _serialMonitor;

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
                _sshMonitor.timer.Enabled = value == VolumeListener.RemoteSystem;
                _state = value;
            }
        }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _logger.LogTrace("Worker started at: {time}", DateTimeOffset.Now);

            _serialHelper = new("COM12", 115200);
            _sshHelper = new("192.168.1.64", 2222, "mobile", "alpine");

            _sshMonitor = new(_sshHelper);
            _sshMonitor.VolumeChanged += SshMonitor_VolumeChanged;

            _volumeMonitor = new();
            _volumeMonitor.MuteChanged += VolumeMonitor_MuteChanged;
            _volumeMonitor.VolumeChanged += VolumeMonitor_VolumeChanged;

            State = VolumeListener.LocalSystem;
        }

        private void VolumeMonitor_MuteChanged(object? sender, VolumeChangedEventArgs e)
        {
            if (_state == VolumeListener.LocalSystem)
            {
                _serialHelper.SetMute(e.Mute);
            }
        }

        private void VolumeMonitor_VolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            if (_state == VolumeListener.LocalSystem)
            {
                _serialHelper.SetLevel((int)e.Volume);
            }
        }

        private void SerialMonitor_LevelChanged(object? sender, FaderLevelChangedEventArgs e)
        {
            if (e.Fader.Touched && State == VolumeListener.LocalSystem)
            {
                _volumeMonitor.SetVolume(e.Fader.Level);
            }
            else if (e.Fader.Touched && State == VolumeListener.RemoteSystem)
            {
                _sshHelper.SetVolume(e.Fader.Level);
            }
        }

        private void SerialMonitor_DoubleClicked(object? sender, FaderLevelChangedEventArgs e)
        {
            State = VolumeListener.LocalSystem;
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
        }

        private void SerialMonitor_TripleClicked(object? sender, FaderLevelChangedEventArgs e)
        {
            State = VolumeListener.RemoteSystem;
        }

        private void SerialMonitor_HeldDown(object? sender, FaderLevelChangedEventArgs e)
        {
            State = VolumeListener.LocalSystem;
        }

        private void SshMonitor_VolumeChanged(object? sender, PhoneChangedEventArgs e)
        {
            if (State == VolumeListener.RemoteSystem && e.Phone.Volume >= 0 && e.Phone.Volume <= 100)
            {
                _serialHelper.SetLevel((int)e.Phone.Volume);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _serialMonitor = new(_serialHelper, stoppingToken);
            _serialMonitor.LevelChanged += SerialMonitor_LevelChanged;
            _serialMonitor.DoubleClicked += SerialMonitor_DoubleClicked;
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