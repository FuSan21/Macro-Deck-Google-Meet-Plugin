namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleHandAction : MeetActionBase
    {
        public override string Name => "Toggle Raised Hand";

        public override string Description => "Raise or lower your hand in Google Meet";

        protected override string EventName => MeetProtocol.Outbound.ToggleHand;
    }
}
