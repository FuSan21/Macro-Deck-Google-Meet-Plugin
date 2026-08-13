namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class TogglePinPresentationAction : MeetActionBase
    {
        public override string Name => "Toggle Pinned Presentation";

        public override string Description =>
            "Pin or unpin someone else's presentation to fill your Google Meet window";

        public override string BindableVariable { get; set; } = "meet_is_presentation_pinned";

        protected override string EventName => MeetProtocol.Outbound.TogglePinPresentation;
    }
}
