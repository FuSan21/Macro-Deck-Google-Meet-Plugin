namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Hides Meet's own toolbars, which is not a Meet feature — the extension simply sets
    /// <c>display: none</c> on them. Handy for a full-screen presentation view.
    /// </summary>
    public class ToggleZenModeAction : MeetActionBase
    {
        public override string Name => "Toggle Zen Mode";

        public override string Description =>
            "Show or hide the Google Meet toolbars. Not an official Meet feature";

        protected override string EventName => MeetProtocol.Outbound.ToggleZenMode;
    }
}
