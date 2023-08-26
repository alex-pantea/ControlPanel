using ControlPanel.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ControlPanel.Core.Factories
{
    public interface IAudioHelperFactory
    {
        IAudioHelper GetAudioHelper();
    }

    public class AudioHelperFactory : IAudioHelperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private static IAudioHelper _audioHelperInstance;

        public AudioHelperFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAudioHelper GetAudioHelper()
        {
            if (_audioHelperInstance != null)
            {
                return _audioHelperInstance;
            }

            Assembly platformAssembly;
            Type? helperType;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.Windows.dll");
                helperType = platformAssembly.GetType("ControlPanel.Core.Windows.CoreAudioHelper");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.Linux.dll");
                helperType = platformAssembly.GetType("ControlPanel.Core.Linux.CoreAudioHelper");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platformAssembly = LoadAssembly("ControlPanel.Core.MacOS.dll");
                helperType = platformAssembly.GetType("ControlPanel.Core.MacOS.CoreAudioHelper");
            }
            else
            {
                throw new NotSupportedException("Audio helper not available for the current platform.");
            }

            if (helperType != null)
            {
                IAudioHelper? helper = (IAudioHelper)ActivatorUtilities.CreateInstance(_serviceProvider, helperType);
                if (helper != null)
                {
                    _audioHelperInstance = helper;
                    return helper;
                }
                else
                {
                    throw new NotSupportedException("Audio helper defined, but not yet implemented for the current platform.");
                }
            }
            else
            {
                throw new NotSupportedException("Audio helper not implemented for the current platform.");
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
