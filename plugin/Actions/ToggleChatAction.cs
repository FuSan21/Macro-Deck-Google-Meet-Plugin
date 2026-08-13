namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleChatAction : MeetActionBase
    {
        public override string Name => "Toggle Chat";

        public override string Description => "Show or hide the chat panel in Google Meet";

        protected override string EventName => MeetProtocol.Outbound.ToggleChat;
    }
}
