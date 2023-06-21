using ControlPanel.Core.Providers;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ControlPanel.Core.Factories
{
    public class VolumeProviderFactory
    {
        public static IVolumeProvider GetVolumeProvider()
        {
            Assembly platformAssembly;
            Type? providerType;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.Windows.dll");
                providerType = platformAssembly.GetType("ControlPanel.Core.Windows.WindowsVolumeProvider");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.Linux.dll");
                providerType = platformAssembly.GetType("ControlPanel.Core.Linux.LinuxVolumeProvider");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.MacOS.dll");
                providerType = platformAssembly.GetType("ControlPanel.Core.MacOS.MacOSVolumeProvider");
            }
            else
            {
                throw new NotSupportedException("Volume provider not available for the current platform.");
            }

            if (providerType != null)
            {
                IVolumeProvider? provider = (IVolumeProvider?)Activator.CreateInstance(providerType);
                if (provider != null)
                {
                    return provider;
                }
                else
                {
                    throw new NotSupportedException("Volume provider defined, but not yet implemented for the current platform.");
                }
            }
            else
            {
                throw new NotSupportedException("Volume provider not implemented for the current platform.");
            }
        }

        private static Assembly LoadAssembly(string assemblyPath)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string assemblyFullPath = Path.Combine(basePath, assemblyPath);

            try
            {
                return Assembly.LoadFrom(assemblyFullPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly '{assemblyPath}': {ex.Message}", ex);
            }
        }
    }
}
