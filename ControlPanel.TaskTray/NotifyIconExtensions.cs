using System.Reflection;

namespace ControlPanel.TaskTray
{
    public static class NotifyIconExtensions
    {
        public static void AddSeperator(this NotifyIcon icon)
        {
            icon.AddItem(new ToolStripSeparator());
        }

        public static ToolStripItem AddLabel(this NotifyIcon icon, string label)
        {
            var item = new ToolStripLabel(label);
            icon.ContextMenuStrip.Items.Add(item);
            return item;
        }

        public static ToolStripItem AddItem(this NotifyIcon icon, string text)
        {
            return icon.ContextMenuStrip.Items.Add(text);
        }

        public static ToolStripItem AddItem(this NotifyIcon icon, string text, EventHandler action)
        {
            var item = icon.AddItem(text);
            item.Click += action;
            return item;
        }

        public static void AddItem(this NotifyIcon icon, ToolStripItem item)
        {
            icon.ContextMenuStrip.Items.Add(item);
        }

        public static void ShowContextMenu(this NotifyIcon icon)
        {
            MethodInfo? mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(icon, null);
        }
    }
}
