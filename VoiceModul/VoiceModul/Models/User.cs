using System.Net;

namespace VoiceModul.Models
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public IPEndPoint? UserEndPoin {  get; set; }
        public User (string userId, string roomID)
        {
            UserId = userId;
            RoomId = roomID;
        }
    }
}
