using ControlPanel.Core.Helpers;
using ControlPanel.Core.Providers;

namespace ControlPanel.Core.Windows
{
    public class WindowsVolumeProvider : IVolumeProvider
    {
        int IVolumeProvider.GetApplicationVolumeLevel(string appName)
        {
            var _app = CoreAudioHelper.GetAudioApps().Where(app => app.ProcessName.Replace(" ", "").Equals(appName, StringComparison.InvariantCultureIgnoreCase));
            if (_app.Any())
            {
                return (int)CoreAudioHelper.GetApplicationVolume((uint)_app.First().Id);
            }
            return -1;
        }

        bool IVolumeProvider.GetApplicationVolumeMute(string appName)
        {
            var _app = CoreAudioHelper.GetAudioApps().Where(app => app.ProcessName.Replace(" ", "").Equals(appName, StringComparison.InvariantCultureIgnoreCase));
            if (_app.Any())
            {
                return CoreAudioHelper.GetApplicationMute((uint)_app.First().Id).Value;
            }
            return true;
        }

        int IVolumeProvider.GetSystemVolumeLevel()
        {
            return (int)Math.Round(CoreAudioHelper.GetMasterVolume());
            throw new NotImplementedException();
        }

        bool IVolumeProvider.GetSystemVolumeMute()
        {
            return CoreAudioHelper.GetMasterVolumeMute();
        }

        void IVolumeProvider.SetApplicationVolumeLevel(string appName, int volume)
        {
            var _app = CoreAudioHelper.GetAudioApps().Where(app => app.ProcessName.Replace(" ", "").Equals(appName, StringComparison.InvariantCultureIgnoreCase));
            if (_app.Any())
            {
                CoreAudioHelper.SetApplicationVolume((uint)_app.First().Id, volume);
            }
        }

        void IVolumeProvider.SetApplicationVolumeMute(string appName, bool mute)
        {
            var _app = CoreAudioHelper.GetAudioApps().Where(app => app.ProcessName.Replace(" ", "").Equals(appName, StringComparison.InvariantCultureIgnoreCase));
            if (_app.Any())
            {
                CoreAudioHelper.SetApplicationMute((uint)_app.First().Id, mute);
            }
        }

        void IVolumeProvider.SetSystemVolumeLevel(int volume)
        {
            CoreAudioHelper.SetMasterVolume(volume);
        }

        void IVolumeProvider.SetSystemVolumeMute(bool mute)
        {
            CoreAudioHelper.SetMasterVolumeMute(mute);
        }
    }
}