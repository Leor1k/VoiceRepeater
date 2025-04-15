using Microsoft.AspNetCore.SignalR;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using VoiceModul.Models;
using VoiceModul.SignalR;

public class VoiceUdpServer
{
    private readonly UdpClient _udpServer;
    private readonly int _port = 5005;
    private readonly RoomManager _roomManager;
    private readonly ConcurrentDictionary<string, User> _userCache = new();
    private readonly IHubContext<VoiceHub> _hubContext; 

    public VoiceUdpServer(RoomManager roomManager, IHubContext<VoiceHub> hubContext)
    {
        _roomManager = roomManager;
        _hubContext = hubContext;

        _udpServer = new UdpClient(_port);
        _udpServer.Client.ReceiveBufferSize = 65536;
        _udpServer.Client.SendBufferSize = 65536;
    }

    public async Task StartListeningAsync()
    {
        Console.WriteLine($"----------UDP сервер запущен на порту {_port}----------");

        while (true)
        {
            try
            {
                UdpReceiveResult result = await _udpServer.ReceiveAsync();
                byte[] receivedData = result.Buffer;
                IPEndPoint sender = result.RemoteEndPoint;

                if (receivedData.Length < 4)
                {
                    Console.WriteLine($"[VoiceUdpServer] Ошибка: слишком короткий пакет от {sender}");
                    continue;
                }

                string userId = BitConverter.ToInt32(receivedData, 0).ToString();
                Console.WriteLine($"[VoiceUdpServer] Получены данные от {sender} (UserId: {userId}), {receivedData.Length} байт");

                string? roomId = _roomManager.GetUserRoomId(userId);
                if (roomId == null)
                {
                    Console.WriteLine($"[VoiceUdpServer] Пользователь {userId} не найден в комнатах. Игнорируем пакет.");
                    continue;
                }

                User? user = _roomManager.GetUserById(userId, roomId);
                if (user == null)
                {
                    Console.WriteLine($"[VoiceUdpServer] Пользователь {userId} не найден в комнате {roomId}. Игнорируем пакет.");
                    continue;
                }

                _userCache.TryAdd(userId, user);

                if (user.UserEndPoin == null || !user.UserEndPoin.Equals(sender))
                {
                    Console.WriteLine($"[VoiceUdpServer] Обновляем EndPoint пользователя {user.UserId} на {sender.Address}:{sender.Port}");
                    _roomManager.UpdateUserEndPoint(user.UserId, user.RoomId, sender);

                    await _hubContext.Clients.Group(user.UserId)
                        .SendAsync("ReceiveUdpPort", sender.Port);

                    Console.WriteLine($"[VoiceUdpServer] Отправлен UDP-порт {sender.Port} пользователю {user.UserId}");
                }

                await _roomManager.BroadcastToRoomAsync(user.RoomId, receivedData, user, _udpServer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoiceUdpServer] Ошибка: {ex.Message}");
            }
        }
    }


}
