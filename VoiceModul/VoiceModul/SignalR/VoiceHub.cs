using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using VoiceModul.Models;
using VoiceModul.Requessts;

namespace VoiceModul.SignalR
{
    public class VoiceHub : Hub
    {
        private static ConcurrentDictionary<string, VoiceCallSession> _activeCalls = new();
        private readonly IHubContext<VoiceHub> _hubContext;
        private readonly RoomManager _roomManager;

        public VoiceHub(IHubContext<VoiceHub> hubContext, RoomManager roomManager)
        {
            _hubContext = hubContext;
            _roomManager = roomManager; 
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                Console.WriteLine($"Пользователь {userId} подключился к VoiceHub");
                Console.WriteLine($"=======================");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                Console.WriteLine($"Пользователь {userId} отключился от VoiceHub");

                string? roomId = _roomManager.GetUserRoomId(userId);
                if (roomId != null)
                {
                    var user = new User(userId, roomId);
                    _roomManager.RemoveClientFromRoom(user);
                    Console.WriteLine($"[OnDisconnectedAsync] Пользователь {userId} удалён из комнаты {roomId}");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task StartCall(CallRequest callRequest)
        {
            Console.WriteLine($"[WebStartCall] Прилетел запрос от {callRequest.CallerId} на создание комнаты");
            var callSession = new VoiceCallSession(callRequest.RoomId, callRequest.CallerId, callRequest.participantIds);
            _activeCalls[callRequest.RoomId] = callSession;
            Console.WriteLine($"[WebStartCall] Пользователь {callRequest.CallerId} начал звонок в комнате {callRequest.RoomId}");

            foreach (var participantId in callRequest.participantIds.Where(id => id != callRequest.CallerId))
            {
                await Clients.Group(participantId).SendAsync("IncomingCall", callRequest.RoomId, callRequest.CallerId);
                Console.WriteLine($"[WebStartCall] С комнаты {callRequest.RoomId} оправляется звонок юзеру с id {participantId} от {callRequest.CallerId}");
            }

            Console.WriteLine($"[WebStartCall] Создана комната {callRequest.RoomId}, активные комнаты: {string.Join(", ", _activeCalls.Keys)}");
        }
    }
}
