using ControlPanel.Core.Providers;
using CoreAudioKit;

namespace ControlPanel.Core.MacOS
{
    public class MacOSVolumeProvider : IVolumeProvider
    {
        public bool GetApplicationMute(string appName)
        {
            throw new NotImplementedException();
        }

        public int GetApplicationVolume(string appName)
        {
            throw new NotImplementedException();
        }

        public int GetSystemLevel()
        {
            throw new NotImplementedException();
        }

        public bool GetSystemMute()
        {
            throw new NotImplementedException();
        }

        public void RemoveApplication(string appName)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationMute(string appName, bool mute)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationVolume(string appName, int volume)
        {
            throw new NotImplementedException();
        }

        public void SetSystemMute(bool mute)
        {
            throw new NotImplementedException();
        }

        public void SetSystemVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public int StepApplicationVolume(string appName, int volume)
        {
            throw new NotImplementedException();
        }

        public int StepSystemVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public bool ToggleApplicationMute(string appName)
        {
            throw new NotImplementedException();
        }

        public bool ToggleSystemMute()
        {
            throw new NotImplementedException();
        }
    }
}