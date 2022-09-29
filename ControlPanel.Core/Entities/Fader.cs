namespace ControlPanel.Core.Entities
{
    public class Fader
    {
        public int Level { get; set; }

        public bool Touched { get; set; }

        public int ClickCount { get; set; }

        public int HoldCount { get; set; }
    }
}
