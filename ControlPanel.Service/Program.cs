IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ControlPanel.Worker>();
    })
    .Build();

await host.RunAsync();
