using ControlPanel.Core.Entities;

namespace ControlPanel.Core.Helpers
{
    public class PlayerTrackMonitor
    {
        private readonly ClientHelper _clientHelper = new();
        private Track _track = new();
        private Player _player = new();


        private void Update(object? _)
        {
            try
            {
                Player = _clientHelper.GetPlayer();
                Track = _clientHelper.GetTrack();
            }
            catch (Exception) { }
        }

        protected Track Track
        {
            get
            {
                return _track;
            }
            set
            {
                if (_track.Id != value.Id && !string.IsNullOrWhiteSpace(value.Id))
                {
                    _track = value;
                    On_TrackChanged();
                }
            }
        }

        protected Player Player
        {
            get
            {
                return _player;
            }
            set
            {
                if (_player.IsPaused && !value.IsPaused)
                {
                    _player = value;
                    On_PlayerResumed();
                }
                else if (!_player.IsPaused && value.IsPaused)
                {
                    _player = value;
                    On_PlayerPaused();
                }
                _player = value;
            }
        }

        public void On_TrackChanged()
        {
            var handler = TrackChanged;
            if (handler != null)
            {
                var args = new PlayerTrackChangedEventArgs() { Player = _player, Track = _track };
                handler(this, args);
            }
        }

        public void On_PlayerPaused()
        {
            var handler = this.PlayerPaused;
            if (handler != null)
            {
                var args = new PlayerTrackChangedEventArgs() { Player = _player, Track = _track };
                handler(this, args);
            }
        }

        public void On_PlayerResumed()
        {
            var handler = this.PlayerResumed;
            if (handler != null)
            {
                var args = new PlayerTrackChangedEventArgs() { Player = _player, Track = _track };
                handler(this, args);
            }
        }

        public PlayerTrackMonitor() { }

        public PlayerTrackMonitor(ClientHelper clientHelper, int delay = 100, int period = 100)
        {
            _clientHelper = clientHelper;
            _ = new Timer(Update, null, delay, period);
        }

        public event EventHandler<PlayerTrackChangedEventArgs>? TrackChanged;

        public event EventHandler<PlayerTrackChangedEventArgs>? PlayerPaused;

        public event EventHandler<PlayerTrackChangedEventArgs>? PlayerResumed;

    }

    public class PlayerTrackChangedEventArgs : EventArgs
    {
        public Track Track { get; set; } = new();
        public Player Player { get; set; } = new();
    }
}
