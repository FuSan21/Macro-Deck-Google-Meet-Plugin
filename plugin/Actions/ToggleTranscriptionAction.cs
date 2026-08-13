namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Starts or stops the meeting transcript. Transcribe is the one tool whose start/stop
    /// control sits on its card rather than inside a sub-panel, so unlike recording it is a
    /// genuine single press — the extension opens the tools panel and presses it.
    /// </summary>
    public class ToggleTranscriptionAction : MeetActionBase
    {
        public override string Name => "Toggle Transcription";

        public override string Description =>
            "Start or stop transcribing the Google Meet call. Needs a Workspace plan that offers transcripts";

        protected override string EventName => MeetProtocol.Outbound.ToggleTranscription;
    }
}
