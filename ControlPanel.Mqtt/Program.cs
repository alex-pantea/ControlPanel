using ControlPanel.Core.Factories;

namespace ControlPanel.Mqtt;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(VolumeProviderFactory.GetVolumeProvider());
                services.AddHostedService<Worker>();
            });
}
