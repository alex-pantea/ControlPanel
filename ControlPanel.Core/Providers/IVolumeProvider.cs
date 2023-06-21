namespace ControlPanel.Core.Providers
{
    public interface IVolumeProvider
    {
        public int GetApplicationVolumeLevel(string appName);
        public bool GetApplicationVolumeMute(string appName);
        public int GetSystemVolumeLevel();
        public bool GetSystemVolumeMute();
        public void SetApplicationVolumeLevel(string appName, int volume);
        public void SetApplicationVolumeMute(string appName, bool mute);
        public void SetSystemVolumeLevel(int volume);
        public void SetSystemVolumeMute(bool mute);
    }
}
