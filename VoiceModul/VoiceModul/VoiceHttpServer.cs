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

        // Добавляем пользователя в комнату
        _roomManager.AddClientToRoom(callerUser);
        Console.WriteLine($"[StartCall] Пользователь {callerUser.UserId} добавлен в комнату {request.RoomId}");

        // Уведомляем участников комнаты о звонке
        request.participantIds ??= new List<string>();

        Console.WriteLine($"[StartCall] Уведомление участников комнаты {request.RoomId} от звонящего {callerUser.UserId}");
        foreach (var part in request.participantIds)
        {
            await _hubContext.Clients.Group(part).SendAsync("IncomingCall", request.RoomId, request.CallerId);
            Console.WriteLine($"[StartCall] Уведомляем участника: {part}");
        }

        Console.WriteLine($"===========[StartCall]===========");
        return Ok($"[StartCall] Комната {request.RoomId} создана.");
    }

    [HttpPost("confirm-call")]
    public IActionResult ConfirmCall([FromBody] CallConfirmation request)
    {
        Console.WriteLine("---------[confirm-call]--------");
        Console.WriteLine($"[ConfirmCall] Принятие звонка пользователем {request.UserId} в комнате {request.RoomId}");

        User confirmUser = new User(request.UserId, request.RoomId);

        _roomManager.AddClientToRoom(confirmUser);
        Console.WriteLine($"[ConfirmCall] Пользователь {confirmUser.UserId} успешно добавлен в {confirmUser.RoomId}");

        Console.WriteLine($"===========[confirm-call]===========");
        return Ok($"[ConfirmCall] Пользователь {confirmUser.UserId} принял приглашение в комнату {confirmUser.RoomId}.");
    }

    [HttpPost("end-call")]
    public IActionResult EndCall([FromBody] CallEndRequest request)
    {
        Console.WriteLine("---------[EndCall]--------");

        User userEnd = new User(request.UserId, request.RoomId);
        Console.WriteLine($"[EndCall] Завершение звонка от {userEnd.UserId} в комнате {userEnd.RoomId}");

        _roomManager.RemoveClientFromRoom(userEnd);

        Console.WriteLine($"===========[EndCall]===========");
        return Ok($"[EndCall] Пользователь {userEnd.UserId} покинул комнату {userEnd.RoomId}.");
    }
}
