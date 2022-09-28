namespace MusicPanel.Core.Entities
{
    public class Player
    {
        public bool HasSong { get; set; }
        public bool IsPaused { get; set; }
        public decimal VolumePercent { get; set; }
        public int SeekbarCurrentPosition { get; set; }
        public string SeekbarCurrentPositionHuman { get; set; } = string.Empty;
        public decimal StatePercent { get; set; }
        public LikeStatus LikeStatus { get; set; } = LikeStatus.INDIFFERENT;
        public RepeatType RepeatType { get; set; } = RepeatType.NONE;

        public bool IsPlaying { get { return HasSong && !IsPaused; } }
    }
    public class Track
    {
        public string Author { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string DurationHuman { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool IsVideo { get; set; }
        public bool IsAdvertisement { get; set; }
        public bool InLibrary { get; set; }
    }

    public enum LikeStatus
    {
        INDIFFERENT,
        LIKE,
        DISLIKE
    }

    public enum RepeatType
    {
        NONE,
        ONE,
        ALL
    }
}
