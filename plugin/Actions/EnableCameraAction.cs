namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class EnableCameraAction : MeetActionBase
    {
        public override string Name => "Turn Camera On";

        public override string Description => "Turn your camera on in Google Meet, whatever state it is in";

        public override string BindableVariable { get; set; } = "meet_is_video_on";

        protected override string EventName => MeetProtocol.Outbound.EnableCamera;
    }
}
