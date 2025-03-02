using VoiceModul.SignalR;
using Microsoft.AspNetCore.SignalR;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5001");

        Console.WriteLine("----------Версия хайп не реальный 6.1.2 Не приходит ничего вообще----------");

        builder.Services.AddSingleton<RoomManager>();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton<VoiceUdpServer>();

        builder.Services.AddHostedService<VoiceUdpBackgroundService>();
        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<VoiceHub>("/voiceHub");
        });

        app.Run();
    }
}
