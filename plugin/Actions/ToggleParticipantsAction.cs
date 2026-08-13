namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    public class ToggleParticipantsAction : MeetActionBase
    {
        public override string Name => "Toggle Participants";

        public override string Description => "Show or hide the participant list in Google Meet";

        protected override string EventName => MeetProtocol.Outbound.ToggleParticipants;
    }
}
