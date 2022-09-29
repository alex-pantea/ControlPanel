using ControlPanel.Core.Entities;

namespace ControlPanel.Core.Helpers
{
    public class SshMonitor
    {
        private readonly SshHelper _sshHelper = new();
        private Phone _phone = new();

        public readonly System.Timers.Timer timer = new();

        private void Update(object? _)
        {
            Phone = _sshHelper.GetPhone();
        }

        protected Phone Phone
        {
            get
            {
                return _phone;
            }
            set
            {
                if (_phone.Volume != value.Volume && value.Volume != -1)
                {
                    _phone = value;
                    On_VolumeChanged();
                }
                if (_phone.Category != value.Category && !string.IsNullOrWhiteSpace(value.Category))
                {
                    _phone = value;
                    On_CategoryChanged();
                }
                _phone = value;
            }
        }

        public void On_VolumeChanged()
        {
            var handler = VolumeChanged;
            if (handler != null)
            {
                var args = new PhoneChangedEventArgs() { Phone = _phone };
                handler(this, args);
            }
        }

        public void On_CategoryChanged()
        {
            var handler = CategoryChanged;
            if (handler != null)
            {
                var args = new PhoneChangedEventArgs() { Phone = _phone };
                handler(this, args);
            }
        }

        public SshMonitor() { }

        public SshMonitor(SshHelper sshHelper, int delay = 100, int period = 1000)
        {
            _sshHelper = sshHelper;
            //_ = new Timer(Update, null, delay, period);
            timer.Interval = period;
            timer.Elapsed += T_Elapsed;
        }

        private void T_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Update(sender);
        }

        public event EventHandler<PhoneChangedEventArgs>? VolumeChanged;
        public event EventHandler<PhoneChangedEventArgs>? CategoryChanged;


    }

    public class PhoneChangedEventArgs : EventArgs
    {
        public Phone Phone { get; set; } = new();
    }
}