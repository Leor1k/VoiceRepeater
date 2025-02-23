using VoiceModul.SignalR;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5001"); // Слушаем на 5001 порту
        Console.WriteLine("----------Версия хайп не реальный 5.2 Есть пробитие порта----------");

        var roomManager = new RoomManager();
        var voiceUdpServer = new VoiceUdpServer(5005, roomManager);

        // Регистрируем сервисы
        builder.Services.AddSingleton(voiceUdpServer);
        builder.Services.AddSingleton(roomManager);  // Добавляем RoomManager как Singleton
        builder.Services.AddHostedService<VoiceUdpBackgroundService>();
        builder.Services.AddControllers();
        builder.Services.AddSignalR(); // Добавляем поддержку SignalR

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<VoiceHub>("/voiceHub"); // Подключаем SignalR хаб
        });

        app.Use(async (context, next) =>
        {
            //Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
            await next();
        });

        app.Run();
    }
}
