using MusicPanel.Core.Entities;

namespace MusicPanel.Core.Helpers
{
    public class SerialMonitor
    {
        private readonly SerialHelper _serialHelper = new();
        private readonly CancellationToken _stoppingToken;

        private Fader _fader = new();

        protected Fader Fader
        {
            get
            {
                return _fader;
            }
            set
            {
                if (_fader.Level != value.Level && value.Level != -1)
                {
                    _fader = value;
                    On_LevelChanged();
                }
                if (_fader.Touched != value.Touched)
                {
                    _fader = value;
                    On_TouchChanged();
                }
                if (_fader.ClickCount != value.ClickCount && value.ClickCount == 2)
                {
                    _fader = value;
                    On_DoubleClicked();
                }
                if (_fader.ClickCount != value.ClickCount && value.ClickCount == 3)
                {
                    _fader = value;
                    On_TripleClicked();
                }
                if (_fader.HoldCount != value.HoldCount && _fader.HoldCount == 2)
                {
                    _fader = value;
                    On_HeldDown();
                }
                _fader = value;
            }
        }

        public void On_LevelChanged()
        {
            var handler = LevelChanged;
            if (handler != null)
            {
                var args = new FaderLevelChangedEventArgs() { Fader = _fader };
                handler(this, args);
            }
        }

        public void On_TouchChanged()
        {
            var handler = TouchChanged;
            if (handler != null)
            {
                var args = new FaderLevelChangedEventArgs() { Fader = _fader };
                handler(this, args);
            }
        }

        public void On_DoubleClicked()
        {
            var handler = DoubleClicked;
            if (handler != null)
            {
                var args = new FaderLevelChangedEventArgs() { Fader = _fader };
                handler(this, args);
            }
        }

        public void On_TripleClicked()
        {
            var handler = TripleClicked;
            if (handler != null)
            {
                var args = new FaderLevelChangedEventArgs() { Fader = _fader };
                handler(this, args);
            }
        }

        public void On_HeldDown()
        {
            var handler = HeldDown;
            if (handler != null)
            {
                var args = new FaderLevelChangedEventArgs() { Fader = _fader };
                handler(this, args);
            }
        }

        public SerialMonitor() { }

        public SerialMonitor(SerialHelper serialHelper, CancellationToken stoppingToken)
        {
            _serialHelper = serialHelper;
            _serialHelper.RequestUpdate();
            _stoppingToken = stoppingToken;

            Thread ReadThread = new(Read);
            ReadThread.Start();
        }

        public event EventHandler<FaderLevelChangedEventArgs>? LevelChanged;
        public event EventHandler<FaderLevelChangedEventArgs>? TouchChanged;
        public event EventHandler<FaderLevelChangedEventArgs>? DoubleClicked;
        public event EventHandler<FaderLevelChangedEventArgs>? TripleClicked;
        public event EventHandler<FaderLevelChangedEventArgs>? HeldDown;

        private void Read()
        {
            while (!_stoppingToken.IsCancellationRequested)
            {
                string fromSerial = _serialHelper.Read();
                foreach (string line in fromSerial.Split("\n"))
                {
                    line.Trim();
                    int level = -1;
                    if (line.StartsWith("LT") &&
                        int.TryParse(line.AsSpan(2), out level) &&
                        level >= 0 && level <= 100)
                    {
                        Fader = new() { Level = level, Touched = true, ClickCount = _fader.ClickCount, HoldCount = _fader.HoldCount };
                    }
                    else if (line.StartsWith("L") &&
                        int.TryParse(line.AsSpan(1), out level) &&
                        level >= 0 && level <= 100)
                    {
                        Fader = new() { Level = level, Touched = false, ClickCount = _fader.ClickCount, HoldCount = _fader.HoldCount };
                    }
                    else if (line.StartsWith("T") && int.TryParse(line.AsSpan(1), out int clicks))
                    {
                        Fader = new() { Level = level, Touched = false, ClickCount = clicks };
                    }
                    else if (line.StartsWith("H") && int.TryParse(line.AsSpan(1), out int holds))
                    {
                        Fader = new() { Level = level, Touched = false, ClickCount = _fader.ClickCount, HoldCount = holds };
                    }
                }
            }
        }
    }

    public class FaderLevelChangedEventArgs : EventArgs
    {
        public Fader Fader { get; set; } = new();
    }
}