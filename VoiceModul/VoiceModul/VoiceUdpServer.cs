using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

public class VoiceUdpServer
{
    private readonly UdpClient _udpServer;
    private readonly int _port;
    private readonly RoomManager _roomManager;
    private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    private readonly Dictionary<IPAddress, int> _clientPorts = new Dictionary<IPAddress, int>(); // Сохранение портов клиентов

    public VoiceUdpServer(int port, RoomManager roomManager)
    {
        _port = port;
        _roomManager = roomManager;
        _udpServer = new UdpClient(_port);
        _udpServer.Client.ReceiveBufferSize = 65536; // 64 KB
        _udpServer.Client.SendBufferSize = 65536;    // 64 KB
    }

    public async Task StartListeningAsync()
    {
        Console.WriteLine($"----------UDP сервер запущен на порту {_port}");

        while (true)
        {
            try
            {
                UdpReceiveResult result = await _udpServer.ReceiveAsync();
                byte[] receivedData = result.Buffer;
                IPEndPoint sender = result.RemoteEndPoint;

                Console.WriteLine($"[VoiceUdpServer] Получены данные от {sender.Address}:{sender.Port}, {receivedData.Length} байт");

                // Сохраняем порт клиента
                if (!_clientPorts.ContainsKey(sender.Address))
                {
                    _clientPorts[sender.Address] = sender.Port;
                    Console.WriteLine($"[VoiceUdpServer] Запомнен порт {sender.Port} для {sender.Address}");
                }

                string roomName = _roomManager.GetRoomForClient(sender);
                if (roomName == null)
                {
                    roomName = "default_room";
                    _roomManager.AddClientToRoom(roomName, sender, _udpServer);
                    Console.WriteLine($"[StartListeningAsync] ---Авто-регистрация клиента {sender} в {roomName}");
                }

                Console.WriteLine($"---- Рассылка пакета от {sender} в комнату {roomName}");
                await _roomManager.BroadcastToRoomAsync(roomName, receivedData, sender, _udpServer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"----Ошибка в UDP-сервере: {ex.Message}");
            }
        }
    }
}
