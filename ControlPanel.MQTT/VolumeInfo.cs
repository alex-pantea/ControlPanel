using ControlPanel.Core.Providers;

namespace ControlPanel.Mqtt
{
    public class VolumeInfo
    {
        private const int MONITOR_DELAY = 200;

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
                _volumeProvider.SetSystemVolume(level);
            }
            else
            {
                _volumeProvider.SetApplicationVolume(Topic, level);
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
                _volumeProvider.SetSystemMute(mute);
            }
            else
            {
                _volumeProvider.SetApplicationMute(Topic, mute);
            }
            _muted = mute;
        }

        private void CheckVolume()
        {
            while (_isRunning)
            {
                // If we've just set the volume within our delay window, 'pause' checking temporarily
                if (_millis > DateTime.Now.Ticks - MONITOR_DELAY * 2)
                {
                    continue;
                }

                try
                {
                    if (Topic == "system")
                    {
                        Volume = _volumeProvider.GetSystemLevel();
                        Muted = _volumeProvider.GetSystemMute();
                    }
                    else
                    {
                        Volume = _volumeProvider.GetApplicationVolume(Topic);
                        Muted = _volumeProvider.GetApplicationMute(Topic);
                    }

                    Thread.Sleep(MONITOR_DELAY);
                }
                catch
                {
                    continue;
                }
            }
            if (Topic != "system")
            {
                _volumeProvider.RemoveApplication(Topic);
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

    public class VolumeChangedEventArgs : EventArgs
    {
        public float Volume { get; set; }
        public bool Mute { get; set; }
    }
}
