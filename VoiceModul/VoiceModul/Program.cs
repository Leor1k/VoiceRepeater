using VoiceModul.SignalR;
using Microsoft.AspNetCore.SignalR;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5001");

        Console.WriteLine("----------Версия хайп не реальный 7.3.1 Так почти всё заработало----------");

        builder.Services.AddSingleton<RoomManager>();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton<VoiceUdpServer>();
        builder.Services.AddSingleton<VoiceHub>();

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
