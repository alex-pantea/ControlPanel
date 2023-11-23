using ControlPanel.Core.Factories;
using ControlPanel.Core.Helpers;
using ControlPanel.Core.Providers;

namespace ControlPanel.Core.Windows
{
    public class WindowsVolumeProvider : IVolumeProvider
    {
        private readonly IAudioHelper _audioHelper;

        public WindowsVolumeProvider(IAudioHelperFactory audioHelperFactory)
        {
            _audioHelper = audioHelperFactory.GetAudioHelper();
        }

        #region System Volume
        int IVolumeProvider.GetSystemLevel()
        {
            return (int)Math.Round(_audioHelper.GetSystemVolume());
        }
        void IVolumeProvider.SetSystemVolume(int volume)
        {
            _audioHelper.SetSystemVolume(volume);
        }
        int IVolumeProvider.StepSystemVolume(int volume)
        {
            return (int)_audioHelper.StepSystemVolume(volume);
        }
        bool IVolumeProvider.GetSystemMute()
        {
            return _audioHelper.GetSystemMute();
        }
        void IVolumeProvider.SetSystemMute(bool mute)
        {
            _audioHelper.SetSystemMute(mute);
        }
        bool IVolumeProvider.ToggleSystemMute()
        {
            return _audioHelper.ToggleSystemMute();
        }
        #endregion

        #region Application Volume
        int IVolumeProvider.GetApplicationVolume(string appName)
        {
            return (int)_audioHelper.GetApplicationVolume(appName);
        }
        void IVolumeProvider.SetApplicationVolume(string appName, int volume)
        {
            _audioHelper.SetApplicationVolume(appName, volume);
        }
        int IVolumeProvider.StepApplicationVolume(string appName, int volume)
        {
            return (int)_audioHelper.StepApplicationVolume(appName, volume);
        }
        bool IVolumeProvider.GetApplicationMute(string appName)
        {
            return _audioHelper.GetApplicationMute(appName);
        }
        void IVolumeProvider.SetApplicationMute(string appName, bool mute)
        {
            _audioHelper.SetApplicationMute(appName, mute);
        }
        bool IVolumeProvider.ToggleApplicationMute(string appName)
        {
            return _audioHelper.ToggleApplicationMute(appName);
        }
        void IVolumeProvider.RemoveApplication(string appName)
        {
            _audioHelper.RemoveApplication(appName);
        }
        #endregion
    }
}