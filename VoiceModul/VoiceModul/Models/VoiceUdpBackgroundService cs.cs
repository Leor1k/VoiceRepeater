public class VoiceUdpBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public VoiceUdpBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var voiceUdpServer = scope.ServiceProvider.GetRequiredService<VoiceUdpServer>(); 
        await voiceUdpServer.StartListeningAsync();
    }
}
