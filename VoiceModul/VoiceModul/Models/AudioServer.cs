using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public static class AudioServer
{
    private static ConcurrentDictionary<string, List<string>> _audioRooms = new();

    public static void CreateRoom(string roomId, List<string> users)
    {
        if (!_audioRooms.ContainsKey(roomId))
        {
            _audioRooms[roomId] = new List<string>(users);
            Console.WriteLine($"Аудио-комната {roomId} создана с пользователями: {string.Join(", ", users)}");
        }
    }

    public static void AddUserToRoom(string roomId, string userId)
    {
        if (_audioRooms.ContainsKey(roomId))
        {
            _audioRooms[roomId].Add(userId);
            Console.WriteLine($"Пользователь {userId} добавлен в аудиокомнату {roomId}");
        }
    }

    public static void RemoveRoom(string roomId)
    {
        if (_audioRooms.ContainsKey(roomId))
        {
            _audioRooms.TryRemove(roomId, out _);
            Console.WriteLine($"Аудио-комната {roomId} удалена.");
        }
    }
}
