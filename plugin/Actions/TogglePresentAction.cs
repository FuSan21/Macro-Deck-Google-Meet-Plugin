namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Starts or stops presenting. This one is only understood by the forked extension
    /// shipped in this repository — with ChrisRegado's official build the command is
    /// received and ignored, and <c>meet_is_presenting</c> stays false.
    ///
    /// Starting a share cannot be completed from here in any case: Chrome will not let a
    /// page pick a screen without the user choosing one in its own picker, so this opens
    /// that picker. Stopping needs no confirmation and completes on its own.
    /// </summary>
    public class TogglePresentAction : MeetActionBase
    {
        public override string Name => "Toggle Presenting";

        public override string Description =>
            "Open the screen-share picker, or stop presenting. Requires this repository's browser extension";

        public override string BindableVariable { get; set; } = "meet_is_presenting";

        protected override string EventName => MeetProtocol.Outbound.TogglePresent;
    }
}
