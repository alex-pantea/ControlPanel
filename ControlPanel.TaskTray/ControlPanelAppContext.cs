using ControlPanel.Core.Helpers;
using System.Diagnostics;
using System.Globalization;
using static System.Windows.Forms.AxHost;

namespace ControlPanel.TaskTray
{
    public class ControlPanelAppContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon = new() { ContextMenuStrip = new() };
        private readonly ToolStripDropDownButton _appMenu = new("Choose App");
        private readonly string[] _ignoredApps = { "Idle", "SignalRgb" };
        private readonly CancellationTokenSource stoppingToken = new();

        private readonly SerialHelper _serialHelper;
        private readonly VolumeMonitor _volumeMonitor;
        protected SerialMonitor _serialMonitor;

        private readonly object _appLock = new();
        private int _appId = -1;
        private bool _masterVolume = true;

        public ControlPanelAppContext()
        {
            // Set icon and visibility
            _notifyIcon.Icon = Properties.Resources.AppIcon;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Control Panel";

            // Show context menu on mouse click as well as right-click
            _notifyIcon.MouseClick += (s, e) =>
            {
                BuildContextMenu();
                if (e.Button == MouseButtons.Left)
                {
                    _notifyIcon.ShowContextMenu();
                }
            };

            _serialHelper = new("COM3", 115200);

            _volumeMonitor = new();
            _volumeMonitor.MuteChanged += VolumeMonitor_MuteChanged;
            _volumeMonitor.VolumeChanged += VolumeMonitor_VolumeChanged;

            _masterVolume = true;

            _serialMonitor = new(_serialHelper, stoppingToken.Token);
            _serialMonitor.LevelChanged += SerialMonitor_LevelChanged;
            _serialMonitor.DoubleClicked += SerialMonitor_DoubleClicked;
        }

        private void AppItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem item)
            {
                lock (_appLock)
                {
                    // If the object chosen is already the active one, stop execution
                    if (int.Parse(item.Name) == _appId)
                    {
                        return;
                    }

                    _masterVolume = item.Text == "System Volume";
                    _appId = int.Parse(item.Name);

                    if (_masterVolume)
                    {
                        _volumeMonitor.SetApp();
                    }
                    else
                    {
                        _volumeMonitor.SetApp(_appId);
                    }
                }
            }
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            stoppingToken.Cancel();

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            ExitThread();
        }

        private void BuildContextMenu()
        {
            _notifyIcon.ContextMenuStrip.Items.Clear();

            _notifyIcon.AddLabel("Control Panel");
            _notifyIcon.AddSeperator();

            // Add items
            AddAudioApps();
            _notifyIcon.AddSeperator();
            ToolStripItem exitMenuItem = _notifyIcon.AddItem("Exit", ExitMenuItem_Click);
        }

        private void AddAudioApps()
        {
            var appItem = _notifyIcon.AddItem("System Volume");
            appItem.Name = "-1";
            appItem.Click += AppItem_Click;
            if (_masterVolume)
            {
                appItem.Font = new Font(appItem.Font, FontStyle.Bold);
            }

            IEnumerable<Process> apps = GetAudioApps();
            if (apps.Any())
            {
                _notifyIcon.AddSeperator();
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                foreach (Process app in apps)
                {
                    if(!string.IsNullOrWhiteSpace(app.MainWindowTitle))
                    {
                        appItem = _notifyIcon.AddItem($"{textInfo.ToTitleCase(app.ProcessName)} | {textInfo.ToTitleCase(app.MainWindowTitle)}");
                    }
                    else
                    {
                        appItem = _notifyIcon.AddItem($"{textInfo.ToTitleCase(app.ProcessName)}");
                    }
                    appItem.Name = app.Id.ToString();
                    appItem.Click += AppItem_Click;
                    if (!_masterVolume && app.Id == _appId)
                    {
                        appItem.Font = new Font(appItem.Font, FontStyle.Bold);
                    }
                }
            }
        }

        private IEnumerable<Process> GetAudioApps()
        {
            foreach (int pid in VolumeHelper.GetVolumeObjects())
            {
                var process = Process.GetProcessById(pid);
                if (!_ignoredApps.Contains(process.ProcessName))
                {
                    yield return process;
                }
            }
        }

        private void VolumeMonitor_MuteChanged(object? sender, VolumeChangedEventArgs e)
        {
            _serialHelper.SetMute(e.Mute);
        }

        private void VolumeMonitor_VolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            if (!_serialMonitor.Fader.Touched)
            {
                _serialHelper.SetLevel((int)e.Volume);
            }
        }

        private void SerialMonitor_LevelChanged(object? sender, FaderLevelChangedEventArgs e)
        {
            if (e.Fader.Touched)
            {
                _volumeMonitor.SetVolume(e.Fader.Level);
            }
        }

        private void SerialMonitor_DoubleClicked(object? sender, FaderLevelChangedEventArgs e)
        {

            if (_masterVolume)
            {
                _masterVolume = false;
                _volumeMonitor.SetApp(_appId);
                
            }
            else
            {
                _masterVolume = true;
                _volumeMonitor.SetApp();
            }
        }
    }
}
