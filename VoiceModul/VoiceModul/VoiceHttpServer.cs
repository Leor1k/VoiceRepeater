using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VoiceModul.Requessts;
using VoiceModul.SignalR;

[ApiController]
[Route("voice")]
public class VoiceController : ControllerBase
{
    private static RoomManager _roomManager = new RoomManager();
    private readonly IHubContext<VoiceHub> _hubContext;

    public VoiceController(IHubContext<VoiceHub> hubContext)
    {
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

        Console.Clear();
        Console.WriteLine("---[StartCall]---");

        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientEndPoint = new IPEndPoint(clientIp, 0);

        Console.WriteLine($"[StartCall] Получен запрос на создание звонка в комнате: {request.RoomId} от {clientIp}");

        if (_roomManager.GetRoomForClient(clientEndPoint) == null)
        {
            Console.WriteLine($"[StartCall] Комната {request.RoomId} не существует. Создаём её.");
            _roomManager.AddClientToRoom(request.RoomId, clientEndPoint, null);
            Console.WriteLine($"[StartCall] Пользователь {clientIp} добавлен в комнату {request.RoomId}");

            request.participantIds ??= new List<string>(); // Фиксируем null participantIds

            foreach (var part in request.participantIds)
            {
                await _hubContext.Clients.Group(part).SendAsync("IncomingCall", request.RoomId, request.CallerId);
                Console.WriteLine($"[StartCall] Уведомляем участников: {string.Join(", ", part)}");
            }

            return Ok($"Комната {request.RoomId} создана.");
        }

        Console.WriteLine($"[StartCall] Комната {request.RoomId} уже существует.");
        return BadRequest("[StartCall] Комната уже существует.");
    }




    [HttpPost("confirm-call")]
    public IActionResult ConfirmCall([FromBody] CallConfirmation request)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress;
            var clientEndPoint = new IPEndPoint(clientIp, 0);

            Console.WriteLine($"[ConfirmCall] Принятие звонка {clientIp} в комнате {request.RoomId}");

            if (_roomManager.GetRoomForClient(clientEndPoint) == null)
            {
                _roomManager.AddClientToRoom(request.RoomId, clientEndPoint, null);
                return Ok($"[ConfirmCall] Пользователь {clientIp} принял приглашение в комнату {request.RoomId}.");
            }

            return NotFound("Комната не найдена.");
        }
        catch (Exception ex)
        {
            return BadRequest($"Ошибка: {ex.Message}");
        }
    }

    [HttpPost("end-call")]
    public IActionResult EndCall([FromBody] CallEndRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress;
        var clientEndPoint = new IPEndPoint(clientIp, 0);

        Console.WriteLine($"[EndCall] Завершение звонка от {clientIp} в комнате {request.RoomId}");

        _roomManager.RemoveClientFromRoom(request.RoomId, clientEndPoint);

        return Ok($"[EndCall] Пользователь {clientIp} покинул комнату {request.RoomId}.");
    }

}
