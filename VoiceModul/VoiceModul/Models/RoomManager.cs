using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System;

public class RoomManager
{
    private ConcurrentDictionary<string, List<IPEndPoint>> _rooms = new();
    private ConcurrentDictionary<IPEndPoint, string> _clientRoomMap = new();
    private readonly SemaphoreSlim _sendSemaphore = new(5); // Ограничение на 5 параллельных отправок
    private readonly Dictionary<IPEndPoint, int> _logCounts = new Dictionary<IPEndPoint, int>();


    public void AddClientToRoom(string roomName, IPEndPoint clientEndPoint, UdpClient udpClient)
    {
        Console.WriteLine($"[AddClientToRoom] Попытка добавить клиента с адресом {clientEndPoint} в комнату {roomName}");

        if (!_rooms.ContainsKey(roomName))
        {
            _rooms[roomName] = new List<IPEndPoint>();
            Console.WriteLine($"[AddClientToRoom] Комната {roomName} не существует, создаём новую.");
        }

        if (!_rooms[roomName].Contains(clientEndPoint))
        {
            _rooms[roomName].Add(clientEndPoint);
            _clientRoomMap[clientEndPoint] = roomName;
            Console.WriteLine($"[AddClientToRoom] Клиент {clientEndPoint} добавлен в комнату {roomName}");
            Console.WriteLine($"[AddClientToRoom] {roomName}] Текущие участники:");
            foreach (var participant in _rooms[roomName])
            {
                Console.WriteLine($"- {participant}");
            }
            if(udpClient != null)
            {
                byte[] testMessage = System.Text.Encoding.UTF8.GetBytes("Welcome to the room from Server");
                udpClient.SendAsync(testMessage, testMessage.Length, clientEndPoint);
                Console.WriteLine($"[AddClientToRoom] отправил {clientEndPoint} тестовое сообщение");
            }
            else
            {
                Console.WriteLine($"[AddClientToRoom] Upd был пуст");
            }
        }
        else
        {
            Console.WriteLine($"[AddClientToRoom] Клиент {clientEndPoint} уже присутствует в комнате {roomName}");
        }
    }


    public void RemoveClientFromRoom(string roomName, IPEndPoint client)
    {
        if (_rooms.ContainsKey(roomName))
        {
            _rooms[roomName].Remove(client);
            Console.WriteLine($"🚪 Клиент {client} покинул комнату {roomName}");

            // Если остался 1 клиент – удаляем комнату
            CheckAndRemoveEmptyRoom(roomName);
        }
    }

    private void CheckAndRemoveEmptyRoom(string roomName)
    {
        if (_rooms.ContainsKey(roomName) && _rooms[roomName].Count <= 1)
        {
            var lastClient = _rooms[roomName].FirstOrDefault(); // Получаем последнего клиента (если есть)

            if (lastClient != null)
            {
                // Отправляем сообщение последнему пользователю, что звонок завершён
                Console.WriteLine($"📢 Звонок в {roomName} завершён, клиент {lastClient} уведомлён");
            }

            // Удаляем комнату
            _rooms.TryRemove(roomName, out _);  // Используем TryRemove для безопасного удаления
            Console.WriteLine($"❌ Комната {roomName} удалена");
        }
    }

    public string GetRoomForClient(IPEndPoint clientEndPoint)
    {
        if (_clientRoomMap.ContainsKey(clientEndPoint))
        {
            string roomName = _clientRoomMap[clientEndPoint];
            Console.WriteLine($"[GetRoomForClient] Клиент {clientEndPoint} находится в комнате {roomName}");
            return roomName;
        }

        Console.WriteLine($"Клиент {clientEndPoint} не найден в комнатах.");
        return null;
    }


    public List<IPEndPoint> GetClientsInRoom(string roomName)
    {
        return _rooms.ContainsKey(roomName) ? _rooms[roomName] : new List<IPEndPoint>();
    }

    public async Task BroadcastToRoomAsync(string roomName, byte[] data, IPEndPoint sender, UdpClient udpClient)
    {
        try
        {
            Console.WriteLine("[BroadcastToRoomAsync] Начало BroadCast");
            // Увеличиваем буфер отправки для UDP-сокета
            udpClient.Client.SendBufferSize = 65536; // 64 KB

            foreach (var client in GetClientsInRoom(roomName))
            {
                if (!client.Equals(sender))  // Не отправляем обратно отправителю
                {
                    await udpClient.SendAsync(data, data.Length, client);
                    Console.WriteLine($"[BroadcastToRoomAsync] Отправка данных на {client} а конкретнее {client.Address}:{client.Port}");

                    // Ограничиваем количество логов
                    if (!_logCounts.ContainsKey(client))
                        _logCounts[client] = 0;

                    if (_logCounts[client] < 3)
                    {
                        Console.WriteLine($"[BroadcastToRoomAsync] Пакет отправлен клиенту {client} с {sender.Address}:{sender.Port}");
                        _logCounts[client]++;
                    }
                }
            }
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }


}
