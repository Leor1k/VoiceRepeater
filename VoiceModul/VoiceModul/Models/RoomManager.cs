using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using VoiceModul.Models;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, List<User>> _rooms = new();

    public void AddClientToRoom(User user)
    {
        Console.WriteLine($"[AddClientToRoom] Попытка добавить пользователя {user.UserId} в комнату {user.RoomId}");

        _rooms.AddOrUpdate(user.RoomId, new List<User> { user }, (key, list) =>
        {
            if (!list.Any(u => u.UserId == user.UserId))
            {
                list.Add(user);
                Console.WriteLine($"[AddClientToRoom] Пользователь {user.UserId} добавлен в комнату {user.RoomId}");
            }
            return list;
        });
    }

    public void RemoveClientFromRoom(User user)
    {
        if (_rooms.ContainsKey(user.RoomId))
        {
            _rooms[user.RoomId].RemoveAll(u => u.UserId == user.UserId);
            Console.WriteLine($"[RemoveClientFromRoom] Пользователь {user.UserId} покинул комнату {user.RoomId}");

            CheckAndRemoveEmptyRoom(user.RoomId);
        }
    }

    private void CheckAndRemoveEmptyRoom(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var users) && users.Count == 0)
        {
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine($"❌ Комната {roomId} удалена");
        }
    }

    public User? GetUserById(string userId, string roomId)
    {
        return _rooms.TryGetValue(roomId, out var users) ? users.FirstOrDefault(u => u.UserId == userId) : null;
    }

    public List<User> GetClientsInRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var users) ? users : new List<User>();
    }
    public string? GetUserRoomId(string userId)
    {
        foreach (var room in _rooms)
        {
            if (room.Value.Any(u => u.UserId == userId))
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
        }
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
                    Console.WriteLine($"[BroadcastToRoomAsync] Отправка данных клиенту {user.UserEndPoin} от {sender.UserEndPoin}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BroadcastToRoomAsync] Ошибка при отправке данных: {ex.Message}");
        }
    }
    public async Task EchoTestAsync(byte[] data, User sender, UdpClient udpClient)
    {
        try
        {
            if (sender.UserEndPoin != null)
            {
                Console.WriteLine($"[EchoTestAsync] Отправка тестового ответа клиенту {sender.UserEndPoin}");

                byte[] response = Encoding.UTF8.GetBytes("TEST_RESPONSE");
                await udpClient.SendAsync(response, response.Length, sender.UserEndPoin);
            }
            else
            {
                Console.WriteLine($"[EchoTestAsync] У клиента {sender.UserId} нет IPEndPoint!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EchoTestAsync] Ошибка при отправке тестового ответа: {ex.Message}");
        }
    }

}
