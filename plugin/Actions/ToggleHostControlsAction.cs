namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleHostControlsAction : MeetActionBase
    {
        public override string Name => "Toggle Host Controls";

        public override string Description =>
            "Show or hide the Google Meet host controls panel. Only the host has one";

        protected override string EventName => MeetProtocol.Outbound.ToggleHostControls;
    }
}
