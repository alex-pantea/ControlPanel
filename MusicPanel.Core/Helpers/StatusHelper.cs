using MusicPanel.Core.Entities;

namespace MusicPanel.Core.Helpers
{
    public static class StatusHelper
    {
        public static bool TrackChanged(this Track? self, Track? other) => self?.Id != other?.Id;
        public static bool PlayerPaused(this Player? self, Player? other) => !self!.IsPaused && other!.IsPaused;
        public static bool PlayerResumed(this Player? self, Player? other) => self!.IsPaused && !other!.IsPaused;
        public static bool PlayerVolumeChanged(this Player? self, Player? other) => self?.VolumePercent != other?.VolumePercent;
        public static bool TrackFinished(this Player? self) => self?.StatePercent == 1;
        public static bool AdPlaying(this Track? self) => self!.IsAdvertisement;
    }
}
