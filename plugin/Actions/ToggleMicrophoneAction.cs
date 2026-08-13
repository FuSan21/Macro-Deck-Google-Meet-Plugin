namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleMicrophoneAction : MeetActionBase
    {
        public override string Name => "Toggle Microphone";

        public override string Description => "Mute or unmute your microphone in Google Meet";

        protected override string EventName => MeetProtocol.Outbound.ToggleMic;
    }
}
