using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VoiceModul.Models;
using VoiceModul.Requessts;
using VoiceModul.SignalR;

[ApiController]
[Route("voice")]
public class VoiceController : ControllerBase
{
    private readonly RoomManager _roomManager;
    private readonly IHubContext<VoiceHub> _hubContext;

    public VoiceController(RoomManager roomManager, IHubContext<VoiceHub> hubContext)
    {
        _roomManager = roomManager;
        _hubContext = hubContext;
    }

    [HttpPost("start-call")]
    public async Task<IActionResult> StartCall([FromBody] CallRequest? request)
    {
        if (request == null)
        {
            Console.WriteLine("[StartCall] Ошибка: request = null");
            return BadRequest("Некорректный запрос.");
        }

        Console.WriteLine("---------[StartCall]--------");

        User callerUser = new User(request.CallerId, request.RoomId);
        Console.WriteLine($"Создан User с {callerUser.UserId}");
        _roomManager.AddClientToRoom(callerUser);
        Console.WriteLine($"[StartCall] Пользователь {callerUser.UserId} добавлен в комнату {request.RoomId}");
        request.participantIds ??= new List<string>();
        Console.WriteLine($"[StartCall] Уведомление участников комнаты {request.RoomId} от звонящего {callerUser.UserId}");
        foreach (var part in request.participantIds)
        {
            await _hubContext.Clients.Group(part).SendAsync("IncomingCall", request.RoomId, request.CallerId);
            Console.WriteLine($"[StartCall] Уведомляем участника: {part}");
        }
        _roomManager.PrintAllRoomsInfo();
        Console.WriteLine($"===========[StartCall]===========");
        return Ok($"[StartCall] Комната {request.RoomId} создана.");

    }

    [HttpPost("confirm-call")]
    public async Task<IActionResult> ConfirmCall([FromBody] CallConfirmation request)
    {
        Console.WriteLine("---------[confirm-call]--------");
        Console.WriteLine($"[ConfirmCall] Принятие звонка пользователем {request.UserId} в комнате {request.RoomId}");

        User confirmUser = new User(request.UserId, request.RoomId);

        _roomManager.AddClientToRoom(confirmUser);
        Console.WriteLine($"[ConfirmCall] Пользователь {confirmUser.UserId} успешно добавлен в {confirmUser.RoomId}");

        var roomUsers = _roomManager.GetClientsInRoom(request.RoomId);

        foreach (var user in roomUsers.Where(u => u.UserId != request.UserId))
        {
            await _hubContext.Clients.Group(user.UserId)
                .SendAsync("UserJoinedCall", request.RoomId, request.UserId);
            Console.WriteLine($"[ConfirmCall] Уведомление UserJoinedCall отправлено пользователю {user.UserId}");
        }

        Console.WriteLine($"===========[confirm-call]===========");
        _roomManager.PrintAllRoomsInfo();
        return Ok($"[ConfirmCall] Пользователь {confirmUser.UserId} принял приглашение в комнату {confirmUser.RoomId}.");
    }


    [HttpPost("end-call")]
    public async Task<IActionResult> EndCall([FromBody] CallEndRequest request)
    {
        Console.WriteLine("---------[EndCall]--------");

        User userEnd = new User(request.UserId, request.RoomId);
        Console.WriteLine($"[EndCall] Завершение звонка от {userEnd.UserId} в комнате {userEnd.RoomId}");
        var roomUsers = _roomManager.GetClientsInRoom(request.RoomId)
                                    .Where(u => u.UserId != request.UserId)
                                    .ToList();
        _roomManager.RemoveClientFromRoom(userEnd);
        foreach (var user in roomUsers)
        {
            await _hubContext.Clients.Group(user.UserId)
                .SendAsync("UserLeftCall", request.RoomId, request.UserId);
            Console.WriteLine($"[EndCall] Уведомление UserLeftCall отправлено пользователю {user.UserId}");
        }

        Console.WriteLine($"===========[EndCall]===========");
        _roomManager.PrintAllRoomsInfo();

        return Ok($"[EndCall] Пользователь {userEnd.UserId} покинул комнату {userEnd.RoomId}.");
    }


    [HttpPost("reject-call")]
    public async Task<IActionResult> RejectCall([FromBody] CallEndRequest request)
    {
        Console.WriteLine("---------[RejectCall]--------");

        User rejectingUser = new User(request.UserId, request.RoomId);
        Console.WriteLine($"[RejectCall] Пользователь {rejectingUser.UserId} отклонил звонок в комнате {rejectingUser.RoomId}");

        var roomUsers = _roomManager.GetClientsInRoom(request.RoomId);

        if (roomUsers.Count == 2)
        {
            var otherUser = roomUsers.FirstOrDefault(u => u.UserId != rejectingUser.UserId);
            if (otherUser != null)
            {
                await _hubContext.Clients.Group(otherUser.UserId).SendAsync("RejectEndCall", request.RoomId, rejectingUser.UserId);
                Console.WriteLine($"[RejectCall] Отправлено RejectEndCall пользователю {otherUser.UserId}");
            }
        }
        else
        {
            foreach (var user in roomUsers.Where(u => u.UserId != rejectingUser.UserId))
            {
                await _hubContext.Clients.Group(user.UserId).SendAsync("RejectUserCall", request.RoomId, rejectingUser.UserId);
                Console.WriteLine($"[RejectCall] Отправлено RejectUserCall пользователю {user.UserId}");
            }
        }

        _roomManager.RemoveClientFromRoom(rejectingUser);

        Console.WriteLine($"===========[RejectCall]===========");
        _roomManager.PrintAllRoomsInfo();
        return Ok($"[RejectCall] Пользователь {rejectingUser.UserId} отклонил звонок в комнате {rejectingUser.RoomId}.");
    }


}
