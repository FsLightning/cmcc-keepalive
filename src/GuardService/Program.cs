using GuardService;
using GuardService.Automation;
using GuardService.Configuration;
using GuardService.Monitoring;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.SingleLine = true;
});

builder.Services
    .AddOptions<GuardOptions>()
    .Bind(builder.Configuration.GetSection("Guard"))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.NormalizedProcessName),
        "Guard:TargetProcessName must contain a valid process or executable name.")
    .ValidateOnStart();

builder.Services.AddSingleton<ProcessMonitor>();
builder.Services.AddSingleton<WindowProbe>();
builder.Services.AddSingleton<SessionClassifier>();
builder.Services.AddSingleton<ProcessController>();
builder.Services.AddSingleton<LoginAssist>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
