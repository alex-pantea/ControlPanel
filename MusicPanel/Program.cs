IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<MusicPanel.Worker>();
    })
    .Build();

await host.RunAsync();
