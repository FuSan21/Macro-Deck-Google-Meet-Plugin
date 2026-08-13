namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class MuteMicrophoneAction : MeetActionBase
    {
        public override string Name => "Mute Microphone";

        public override string Description => "Mute your microphone in Google Meet, whatever state it is in";

        public override string BindableVariable { get; set; } = "meet_is_muted";

        protected override string EventName => MeetProtocol.Outbound.MuteMic;
    }
}
