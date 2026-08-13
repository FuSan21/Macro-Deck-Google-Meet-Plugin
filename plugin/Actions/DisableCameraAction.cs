namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class DisableCameraAction : MeetActionBase
    {
        public override string Name => "Turn Camera Off";

        public override string Description => "Turn your camera off in Google Meet, whatever state it is in";

        protected override string EventName => MeetProtocol.Outbound.DisableCamera;
    }
}
