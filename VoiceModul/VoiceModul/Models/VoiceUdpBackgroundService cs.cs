
public class VoiceUdpBackgroundService : BackgroundService
{
    private readonly VoiceUdpServer _voiceUdpServer;

    public VoiceUdpBackgroundService(VoiceUdpServer voiceUdpServer)
    {
        _voiceUdpServer = voiceUdpServer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Запуск UDP-сервера, если токен не отменен
        await _voiceUdpServer.StartListeningAsync();
    }
}
