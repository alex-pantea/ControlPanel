using ControlPanel.Core.Helpers;

namespace ControlPanel.Core.Providers
{
    public interface IVolumeProvider
    {
        #region System Volume
        public int GetSystemLevel();
        public void SetSystemVolume(int volume);
        public int StepSystemVolume(int volume);
        public bool GetSystemMute();
        public void SetSystemMute(bool mute);
        public bool ToggleSystemMute();
        #endregion

        #region Application Volume
        public int GetApplicationVolume(string appName);
        public void SetApplicationVolume(string appName, int volume);
        public int StepApplicationVolume(string appName, int volume);
        public bool GetApplicationMute(string appName);
        public void SetApplicationMute(string appName, bool mute);
        public bool ToggleApplicationMute(string appName);
        #endregion
    }
}
