using System.Diagnostics;
using CoreAudio;

namespace ControlPanel.Core.Helpers
{
    public static class CoreAudioHelper
    {
        private static readonly string[] IGNORED_APPS = 
            { "idle", "signalrgb", "steamwebhelper", "steam", "parsecd", "mstsc", "brave", "atmgr", "msedgewebview2", "wsaclient", "windowsterminal" };

        #region Master Volume Manipulation

        /// <summary>
        /// Gets the current master volume in scalar values (percentage)
        /// </summary>
        /// <returns>-1 in case of an error, if successful the value will be between 0 and 100</returns>
        public static float GetMasterVolume()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return -1;

            return masterVol.MasterVolumeLevelScalar * 100;
        }

        /// <summary>
        /// Gets the mute state of the master volume. 
        /// While the volume can be muted the <see cref="GetMasterVolume"/> will still return the pre-muted volume value.
        /// </summary>
        /// <returns>false if not muted, true if volume is muted</returns>
        public static bool GetMasterVolumeMute()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return false;

            return masterVol.Mute;
        }

        /// <summary>
        /// Sets the master volume to a specific level
        /// </summary>
        /// <param name="newLevel">Value between 0 and 100 indicating the desired scalar value of the volume</param>
        public static void SetMasterVolume(float newLevel)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null || newLevel < 0 || newLevel > 100)
                return;

            masterVol.MasterVolumeLevelScalar = newLevel / 100;
        }

        /// <summary>
        /// Increments or decrements the current volume level by the <see cref="stepAmount"/>.
        /// </summary>
        /// <param name="stepAmount">Value between -100 and 100 indicating the desired step amount. Use negative numbers to decrease
        /// the volume and positive numbers to increase it.</param>
        /// <returns>the new volume level assigned</returns>
        public static float StepMasterVolume(float stepAmount)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return -1;

            float stepAmountScaled = stepAmount / 100;

            // Get the level
            float volumeLevel = masterVol.MasterVolumeLevelScalar;

            // Calculate the new level
            float newLevel = volumeLevel + stepAmountScaled;
            newLevel = Math.Min(1, newLevel);
            newLevel = Math.Max(0, newLevel);

            masterVol.MasterVolumeLevelScalar = newLevel;

            // Return the new volume level that was set
            return newLevel * 100;
        }

        /// <summary>
        /// Mute or unmute the master volume
        /// </summary>
        /// <param name="isMuted">true to mute the master volume, false to unmute</param>
        public static void SetMasterVolumeMute(bool isMuted)
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return;

            masterVol.Mute = isMuted;
        }

        /// <summary>
        /// Switches between the master volume mute states depending on the current state
        /// </summary>
        /// <returns>the current mute state, true if the volume was muted, false if unmuted</returns>
        public static bool ToggleMasterVolumeMute()
        {
            AudioEndpointVolume? masterVol = GetMasterVolumeObject();
            if (masterVol == null)
                return false;

            masterVol.Mute = !masterVol.Mute;

            return masterVol.Mute;
        }

        public static AudioEndpointVolume? GetMasterVolumeObject()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            return speakers.AudioEndpointVolume;
        }

        public static AudioSessionControl2? GetMasterVolumeSession()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (speakers.AudioSessionManager2 == null)
            {
                return null;
            }
            else if (speakers.AudioSessionManager2.Sessions == null || speakers.AudioSessionManager2.Sessions.Count == 0)
            {
                return null;
            }

            return speakers.AudioSessionManager2.Sessions[0];
        }
        #endregion

        #region Individual Application Volume Manipulation

        public static float? GetApplicationVolume(uint pid)
        {
            SimpleAudioVolume? volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            return volume.MasterVolume * 100;
        }

        public static bool? GetApplicationMute(uint pid)
        {
            SimpleAudioVolume? volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            return volume.Mute;
        }

        public static void SetApplicationVolume(uint pid, float level)
        {
            SimpleAudioVolume? volume = GetVolumeObject(pid);
            if (volume == null || level < 0 || level > 100)
                return;

            volume.MasterVolume = level / 100;
        }

        public static void SetApplicationMute(uint pid, bool mute)
        {
            SimpleAudioVolume? volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            volume.Mute = mute;
        }

        private static SimpleAudioVolume? GetVolumeObject(uint pid)
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDeviceCollection speakers = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            SimpleAudioVolume? volume = null;
            foreach (var speaker in speakers)
            {
                AudioSessionManager2? mgr = speaker.AudioSessionManager2;
                if (mgr == null)
                {
                    continue;
                }
                SessionCollection? sessions = mgr.Sessions;
                if (sessions == null)
                {
                    continue;
                }

                foreach (var session in sessions)
                {
                    if (session.ProcessID == pid)
                    {
                        volume = session.SimpleAudioVolume;
                        break;
                    }
                }
            }
            return volume;
        }

        public static AudioSessionControl2? GetApplicationVolumeSession(uint pid)
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDeviceCollection speakers = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            AudioSessionControl2? session = null;
            foreach (var speaker in speakers)
            {
                AudioSessionManager2? mgr = speaker.AudioSessionManager2;
                if (mgr == null)
                {
                    continue;
                }
                SessionCollection? sessions = mgr.Sessions;
                if (sessions == null || sessions.Count == 0)
                {
                    continue;
                }

                foreach (var s in sessions)
                {
                    if (s.ProcessID == pid)
                    {
                        session = s;
                        break;
                    }
                }
            }
            return session;
        }

        public static IEnumerable<uint> GetVolumeObjects()
        {
            MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
            MMDevice speaker = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            AudioSessionManager2? mgr = speaker.AudioSessionManager2;
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
                yield return session.ProcessID;
            }
        }

        public static IEnumerable<Process> GetAudioApps()
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
                catch { }
                if (process != null)
                {
                    yield return process;
                }
            }
        }
        #endregion
    }
}
