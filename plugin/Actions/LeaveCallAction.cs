namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Leaves the call. Meetings that ask whether you want to leave or end the call for
    /// everyone need a second press: the first opens the dialog, the second picks "leave".
    /// </summary>
    public class LeaveCallAction : MeetActionBase
    {
        public override string Name => "Leave Call";

        public override string Description =>
            "Leave the Google Meet call. If Meet asks to confirm, press again to leave";

        protected override string EventName => MeetProtocol.Outbound.LeaveCall;
    }
}
