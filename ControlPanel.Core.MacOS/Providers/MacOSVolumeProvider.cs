using ControlPanel.Core.Providers;
using CoreAudioKit;

namespace ControlPanel.Core.MacOS
{
    public class MacOSVolumeProvider : IVolumeProvider
    {
        int IVolumeProvider.GetApplicationVolumeLevel(string appName)
        {
            throw new NotImplementedException();
        }

        bool IVolumeProvider.GetApplicationVolumeMute(string appName)
        {
            throw new NotImplementedException();
        }

        int IVolumeProvider.GetSystemVolumeLevel()
        {
            throw new NotImplementedException();
        }

        bool IVolumeProvider.GetSystemVolumeMute()
        {
            throw new NotImplementedException();
        }

        void IVolumeProvider.SetApplicationVolumeLevel(string appName, int volume)
        {
            throw new NotImplementedException();
        }

        void IVolumeProvider.SetApplicationVolumeMute(string appName, bool mute)
        {
            throw new NotImplementedException();
        }

        void IVolumeProvider.SetSystemVolumeLevel(int volume)
        {
            throw new NotImplementedException();
        }

        void IVolumeProvider.SetSystemVolumeMute(bool mute)
        {
            throw new NotImplementedException();
        }
    }
}