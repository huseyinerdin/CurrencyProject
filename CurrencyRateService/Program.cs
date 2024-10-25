using CurrencyRateService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        services.Configure<EmailSettings>(hostContext.Configuration.GetSection("EmailSettings"));

    })
    .Build();

await host.RunAsync();
