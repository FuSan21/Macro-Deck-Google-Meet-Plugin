namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleMeetingToolsAction : MeetActionBase
    {
        public override string Name => "Toggle Meeting Tools";

        public override string Description => "Show or hide the Google Meet tools side panel";

        protected override string EventName => MeetProtocol.Outbound.ToggleMeetingTools;
    }
}
