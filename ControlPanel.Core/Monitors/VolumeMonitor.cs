using System.Diagnostics;
using System.Security.Cryptography;

namespace ControlPanel.Core.Helpers
{
    public class VolumeMonitor
    {
        private readonly Timer _timer = null!;
        private readonly object _pidLock = new();

        private int _pid = -1;
        private bool _mute = false;
        private int _volume = -1;

        private void Update(object? _)
        {
            lock (_pidLock)
            {
                if (_pid == -1)
                {
                    Volume = (int)VolumeHelper.GetMasterVolume();
                    Mute = VolumeHelper.GetMasterVolumeMute();
                }
                else
                {
                    try
                    {
                        Volume = (int)VolumeHelper.GetApplicationVolume(_pid)!.Value;
                        Mute = VolumeHelper.GetApplicationMute(_pid)!.Value;
                    }
                    catch (Exception) { }
                }
            }
        }

        protected int Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value && value != -1) // -1 is returned if the AudioManager fails
                {
                    _volume = value;
                    On_VolumeChanged();
                }
            }
        }

        protected bool Mute
        {
            get
            {
                return _mute;
            }
            set
            {
                if (_mute != value)
                {
                    _mute = value;
                    On_MuteChanged();
                }
            }
        }

        public void On_VolumeChanged()
        {
            var handler = VolumeChanged;
            if (handler != null)
            {
                var args = new VolumeChangedEventArgs() { Volume = _volume, Mute = _mute };
                handler(this, args);
            }
        }

        public void On_MuteChanged()
        {
            var handler = MuteChanged;
            if (handler != null)
            {
                var args = new VolumeChangedEventArgs() { Volume = _volume, Mute = _mute };
                handler(this, args);
            }
        }

        public void SetVolume(int volume)
        {
            _volume = volume;
            lock (_pidLock)
            {
                if (_pid == -1)
                {
                    VolumeHelper.SetMasterVolume(volume);
                }
                else
                {
                    VolumeHelper.SetApplicationVolume(_pid, volume);
                }
            }
        }

        public void ToggleMute()
        {
            _mute = !_mute;
            lock (_pidLock)
            {
                if (_pid == -1)
                {
                    VolumeHelper.SetMasterVolumeMute(_mute);
                }
                else
                {
                    VolumeHelper.SetApplicationMute(_pid, _mute);
                }
            }
        }

        public VolumeMonitor(int delay = 100, int period = 100)
        {
            _timer = new Timer(Update, null, delay, period);
        }

        public void SetApp(string appName)
        {
            lock (_pidLock)
            {
                if (!string.IsNullOrWhiteSpace(appName))
                {
                    try
                    {
                        _pid = Process.GetProcessesByName(appName).Select(p => p.Id).Intersect(VolumeHelper.GetVolumeObjects()).First();
                    }
                    catch (Exception)
                    {
                        throw new ArgumentOutOfRangeException(nameof(appName), appName, "Process was not found or is not running");
                    }
                }
                else
                {
                    _pid = -1;
                }
            }
        }

        public void SetApp(int pid = -1)
        {
            lock (_pidLock)
            {
                _pid = pid;
            }
        }

        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;
        public event EventHandler<VolumeChangedEventArgs>? MuteChanged;
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public float Volume { get; set; }
        public bool Mute { get; set; }
    }
}