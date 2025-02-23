namespace VoiceModul.Requessts
{
    public class CallRequest
    {
        public string CallerId {  get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public List<string> participantIds { get; set; } = null;
    }
}
