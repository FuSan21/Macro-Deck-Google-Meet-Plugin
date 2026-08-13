namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleMeetingDetailsAction : MeetActionBase
    {
        public override string Name => "Toggle Meeting Details";

        public override string Description =>
            "Show or hide the Google Meet details panel, which holds the joining link";

        protected override string EventName => MeetProtocol.Outbound.ToggleMeetingDetails;
    }
}
