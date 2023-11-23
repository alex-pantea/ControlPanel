using ControlPanel.Core.Helpers;
using CoreAudio;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ControlPanel.Core.Windows
{
    public class CoreAudioHelper : IAudioHelper
    {
        private static readonly string[] IGNORED_APPS =
            { "idle", "signalrgb", "steamwebhelper", "steam", "parsecd", "mstsc", "brave",
              "atmgr", "msedgewebview2", "wsaclient", "windowsterminal", "hass.agent" };
        private readonly Dictionary<(string DeviceFriendlyName, string AppName), SimpleAudioVolume> _deviceSessions = new();
        private string _defaultSpeakerID = string.Empty;
        private readonly ILogger _logger;
        private AudioCurveHelper _curveHelper;

        public CoreAudioHelper(ILogger<IAudioHelper> logger)
        {
            _logger = logger;
            _curveHelper = new AudioCurveHelper();
        }

        #region Audio Curves
        public void AddCurve(int point, int value)
        {
            _curveHelper.AddValue(point, value);
        }
        public float ConvertToCurve(float value)
        {
            return _curveHelper.Interpolate(value);
        }
        public float ConvertFromCurve(float value)
        {
            return _curveHelper.DeInterpolate(value);
        }
        #endregion

        #region Output Device
        public void SetDefaultAudioEndpoint(string defaultAudioEndpoint)
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            //using MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            deviceEnumerator.SetDefaultAudioEndpoint(deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).First(d => d.DeviceFriendlyName == defaultAudioEndpoint));
        }

        public string GetDefaultAudioEndpoint()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            using MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            return speakers.DeviceFriendlyName;
        }

        public void SetAppAudioEndpoint(string appName, string audioEndpoint)
        {

        }

        public string GetAppAudioEndpoint(string appName)
        {
            return string.Empty;
        }

        #endregion

        #region System Volume
        public float GetSystemVolume()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return -1;

            return ConvertFromCurve(masterVol.MasterVolumeLevelScalar * 100);
        }

        public void SetSystemVolume(float newLevel)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null || newLevel < 0 || newLevel > 100)
                return;

            masterVol.MasterVolumeLevelScalar = ConvertToCurve(newLevel) / 100;
        }

        public float StepSystemVolume(float stepAmount)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return -1;

            float stepAmountScaled = stepAmount / 100;

            // Get the level
            float volumeLevel = masterVol.MasterVolumeLevelScalar;

            // Calculate the new level
            float newLevel = volumeLevel + stepAmountScaled;
            newLevel = Math.Clamp(newLevel, 0, 1);

            masterVol.MasterVolumeLevelScalar = newLevel;

            // Return the new volume level that was set
            return newLevel * 100;
        }

        public bool GetSystemMute()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return false;

            return masterVol.Mute;
        }

        public void SetSystemMute(bool isMuted)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return;

            masterVol.Mute = isMuted;
        }

        public bool ToggleSystemMute()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return false;

            masterVol.Mute = !masterVol.Mute;

            return masterVol.Mute;
        }

        private AudioEndpointVolume? GetMasterVolumeObject()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            using MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // When the default audio endpoint is changed, all app volume objects need to be refreshed
            if (_defaultSpeakerID != speakers.DeviceFriendlyName)
            {
                _logger.LogInformation("Speakers changed to {device}.", speakers.DeviceFriendlyName);
                _defaultSpeakerID = speakers.DeviceFriendlyName;
            }

            return speakers.AudioEndpointVolume;
        }
        #endregion

        #region Application Volume
        private bool GetAppSession(string appName)
        {
            if (!_deviceSessions.Keys.Any(k => k.AppName == appName))
            {
                var deviceSessions = GetVolumeObject(appName);
                foreach (var session in deviceSessions)
                {
                    var key = (DeviceFriendlyName: session.speaker, AppName: appName);
                    if (session.volume != null)
                    {
                        _deviceSessions.Add(key, session.volume);
                    }
                }
            }

            if(!_deviceSessions.Keys.Any(key => key.AppName == appName))
            {
                throw new ApplicationException("audio session not found for this application.");
            }

            return _deviceSessions.Keys.Any(k => k.AppName == appName);
        }

        public float GetApplicationVolume(string appName)
        {
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);
            try
            {
                if (GetAppSession(appName))
                {
                    return ConvertFromCurve(_deviceSessions[key].MasterVolume * 100);
                } else
                {
                    throw new Exception();
                }
            }
            catch
            {
                _deviceSessions.Remove(key);
                throw new ApplicationException("error using the app session for this application.");
            }
        }

        public void SetApplicationVolume(string appName, float level)
        {
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);
            try
            {
                if (GetAppSession(appName))
                {
                    _logger.LogTrace("setting app to {level:000}. level value is {level:000}", _curveHelper.Interpolate(level), level);
                    _deviceSessions[key].MasterVolume = ConvertToCurve(level) / 100;
                }
            }
            catch
            {
                _deviceSessions.Remove(key);
                throw new ApplicationException("error using the app session for this application.");
            }
        }

        public float StepApplicationVolume(string appName, float stepAmount)
        {
            if (!GetAppSession(appName))
            {
                return 0;
            }
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);

            float stepAmountScaled = stepAmount / 100;

            // Get the level
            float volumeLevel = _deviceSessions[key].MasterVolume;

            // Calculate the new level
            float newLevel = volumeLevel + stepAmountScaled;
            newLevel = Math.Clamp(newLevel, 0, 1);

            try
            {
                _deviceSessions[key].MasterVolume = newLevel;

                // Return the new volume level that was set
                return newLevel * 100;
            }
            catch
            {
                _deviceSessions.Remove(key);
                return 0;
            }
        }

        public bool GetApplicationMute(string appName)
        {
            if (!GetAppSession(appName))
            {
                return false;
            }
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);

            try
            {
                return _deviceSessions[key].Mute;
            }
            catch
            {
                _deviceSessions.Remove(key);
                return false;
            }
        }

        public void SetApplicationMute(string appName, bool mute)
        {
            if (!GetAppSession(appName))
            {
                return;
            }
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);

            try
            {
                _deviceSessions[key].Mute = mute;
            }
            catch
            {
                _deviceSessions.Remove(key);
            }
        }

        public bool ToggleApplicationMute(string appName)
        {
            if (!GetAppSession(appName))
            {
                return false;
            }
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);

            try
            {
                _deviceSessions[key].Mute = !_deviceSessions[key].Mute;
                return _deviceSessions[key].Mute;
            }
            catch
            {
                _deviceSessions.Remove(key);
                return false;
            }
        }

        private static IEnumerable<SessionData> GetVolumeObject(string appName)
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDeviceCollection speakers = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var speaker in speakers)
            {
                using (speaker)
                {
                    using AudioSessionManager2? mgr = speaker.AudioSessionManager2;
                    if (mgr == null)
                    {
                        continue;
                    }
                    SessionCollection? sessions = mgr.Sessions;
                    if (sessions == null)
                    {
                        continue;
                    }

                    if (sessions.Any(s => Process.GetProcessById((int)s.ProcessID).ProcessName.Replace(" ", "") == appName))
                    {

                        foreach (var session in sessions.Where(s => Process.GetProcessById((int)s.ProcessID).ProcessName.Replace(" ", "") == appName))
                        {
                            using (session)
                            {
                                yield return new SessionData()
                                {
                                    speaker = speaker.DeviceFriendlyName,
                                    volume = session.SimpleAudioVolume
                                };
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<uint> GetVolumeObjects()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            using MMDevice speaker = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            using AudioSessionManager2? mgr = speaker.AudioSessionManager2;

            if (mgr == null)
            {
                yield break;
            }
            SessionCollection? sessions = mgr.Sessions;
            if (sessions == null)
            {
                yield break;
            }

            foreach (var session in sessions)
            {
                using (session)
                {
                    yield return session.ProcessID;
                }
            }
        }

        public IEnumerable<string> GetAudioApps()
        {
            foreach (uint pid in GetVolumeObjects().Distinct())
            {
                Process? process = null;
                try
                {
                    process = Process.GetProcessById((int)pid);
                    if (IGNORED_APPS.Contains(process.ProcessName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        process = null;
                    }
                }
                catch
                {
                    yield break;
                }
                if (process != null)
                {
                    yield return process.ProcessName.Replace(" ", "");
                }
            }
        }

        public void RemoveApplication(string appName)
        {
            var key = (DeviceFriendlyName: _defaultSpeakerID, AppName: appName);
            _deviceSessions.Remove(key);
        }
        #endregion
    }

    public class SessionData
    {
        public string speaker = string.Empty;
        public SimpleAudioVolume? volume;
    }
}
