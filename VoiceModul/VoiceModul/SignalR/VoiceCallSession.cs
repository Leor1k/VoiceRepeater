namespace VoiceModul.SignalR
{
    public class VoiceCallSession
    {
        public string RoomId { get; }
        public string CallerId { get; }
        private List<string> Participants { get; }
        private HashSet<string> AcceptedUsers { get; } = new();
        private HashSet<string> RejectedUsers { get; } = new();

        public VoiceCallSession(string roomId, string callerId, List<string> participants)
        {
            RoomId = roomId;
            CallerId = callerId;
            Participants = participants;
        }

        public void AcceptCall(string userId) => AcceptedUsers.Add(userId);
        public void RejectCall(string userId) => RejectedUsers.Add(userId);
        public bool HasAcceptedUsers() => AcceptedUsers.Count > 0;
        public List<string> GetAcceptedUsers() => AcceptedUsers.ToList();
    }
}
