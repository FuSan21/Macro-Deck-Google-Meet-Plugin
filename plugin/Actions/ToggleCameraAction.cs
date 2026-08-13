namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleCameraAction : MeetActionBase
    {
        public override string Name => "Toggle Camera";

        public override string Description => "Turn your camera on or off in Google Meet";

        protected override string EventName => MeetProtocol.Outbound.ToggleCamera;
    }
}
