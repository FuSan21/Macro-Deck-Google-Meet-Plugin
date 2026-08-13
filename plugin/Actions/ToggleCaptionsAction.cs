namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleCaptionsAction : MeetActionBase
    {
        public override string Name => "Toggle Captions";

        public override string Description => "Turn live captions on or off in Google Meet";

        public override string BindableVariable { get; set; } = "meet_are_captions_on";

        protected override string EventName => MeetProtocol.Outbound.ToggleCaptions;
    }
}
