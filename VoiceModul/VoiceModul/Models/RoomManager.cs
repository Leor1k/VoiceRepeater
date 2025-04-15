using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using VoiceModul.Models;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, User>> _rooms = new();

    public void AddClientToRoom(User user)
    {
        Console.WriteLine($"[AddClientToRoom] Попытка добавить пользователя {user.UserId} в комнату {user.RoomId}");

        var users = _rooms.GetOrAdd(user.RoomId, _ => new ConcurrentDictionary<string, User>());
        if (users.TryAdd(user.UserId, user))
        {
            Console.WriteLine($"[AddClientToRoom] Пользователь {user.UserId} добавлен в комнату {user.RoomId}");
        }
        else
        {
            Console.WriteLine($"[AddClientToRoom] Пользователь {user.UserId} уже существует в комнате {user.RoomId}");
        }
    }

    public void RemoveClientFromRoom(User user)
    {
        if (_rooms.TryGetValue(user.RoomId, out var users))
        {
            users.TryRemove(user.UserId, out _);
            Console.WriteLine($"[RemoveClientFromRoom] Пользователь {user.UserId} покинул комнату {user.RoomId}");

            if (users.Count == 1)
            {
                var lastUser = users.Values.FirstOrDefault();
                if (lastUser != null)
                {
                    users.TryRemove(lastUser.UserId, out _);
                    Console.WriteLine($"[RemoveClientFromRoom] Последний пользователь {lastUser.UserId} исключён из комнаты {user.RoomId}");
                }
            }

            CheckAndRemoveEmptyRoom(user.RoomId);
        }
    }

    private void CheckAndRemoveEmptyRoom(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var users) && users.IsEmpty)
        {
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine($"❌ Комната {roomId} удалена");
        }
    }

    public User? GetUserById(string userId, string roomId)
    {
        return _rooms.TryGetValue(roomId, out var users) && users.TryGetValue(userId, out var user)
            ? user
            : null;
    }

    public List<User> GetClientsInRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var users)
            ? users.Values.ToList()
            : new List<User>();
    }

    public string? GetUserRoomId(string userId)
    {
        foreach (var room in _rooms)
        {
            if (room.Value.ContainsKey(userId))
            {
                return room.Key;
            }
        }
        return null;
    }

    public void UpdateUserEndPoint(string userId, string roomId, IPEndPoint endPoint)
    {
        var user = GetUserById(userId, roomId);
        if (user != null)
        {
            user.UserEndPoin = endPoint;
            Console.WriteLine($"[UpdateUserEndPoint] Обновлён IPEndPoint для {user.UserId}: {endPoint}");
            PrintAllRoomsInfo();
        }
    }

    public void PrintAllRoomsInfo()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔════════════════════════════════════╗");
        Console.WriteLine("║ ТЕКУЩЕЕ СОСТОЯНИЕ КОМНАТ И УЧАСТНИКОВ ║");
        Console.WriteLine("╚════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine($"Всего комнат: {_rooms.Count}\n");

        foreach (var room in _rooms.OrderBy(r => r.Key))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[[ КОМНАТА {room.Key} ]]");
            Console.ResetColor();

            if (!room.Value.Any())
            {
                Console.WriteLine("  (нет участников)");
                continue;
            }

            foreach (var user in room.Value.Values.OrderBy(u => u.UserId))
            {
                Console.WriteLine($"\n  Пользователь: [ID: {user.UserId}]");

                if (user.UserEndPoin != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ├─ EndPoint: {user.UserEndPoin.Address}:{user.UserEndPoin.Port}");
                    Console.WriteLine($"  └─ Статус: активен");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  └─ EndPoint: не установлен");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    public async Task BroadcastToRoomAsync(string roomId, byte[] data, User sender, UdpClient udpClient)
    {
        try
        {
            Console.WriteLine($"[BroadcastToRoomAsync] Начало отправки в комнату {roomId}");

            foreach (var user in GetClientsInRoom(roomId))
            {
                if (user.UserId != sender.UserId && user.UserEndPoin != null)
                {
                    await udpClient.SendAsync(data, data.Length, user.UserEndPoin);
                    Console.WriteLine($"[BroadcastToRoomAsync] Отправка данных клиенту {user.UserEndPoin} от {sender.UserEndPoin} в комнату {roomId}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastToRoomAsync] Ошибка при отправке данных: {ex.Message}");
        }
    }
}
