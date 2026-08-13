namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class UnmuteMicrophoneAction : MeetActionBase
    {
        public override string Name => "Unmute Microphone";

        public override string Description => "Unmute your microphone in Google Meet, whatever state it is in";

        public override string BindableVariable { get; set; } = "meet_is_muted";

        protected override string EventName => MeetProtocol.Outbound.UnmuteMic;
    }
}
