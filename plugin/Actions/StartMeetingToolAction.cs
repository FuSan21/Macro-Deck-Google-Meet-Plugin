using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Presses a tool's main button in one go, rather than just navigating to it:
    ///
    ///   Record             → Start recording
    ///   Transcribe         → Start transcription
    ///   Polls              → Start a poll
    ///   Q&amp;A               → Ask a question
    ///   Breakout rooms     → Set up breakout rooms
    ///   Speech translation → Enable translation for everyone
    ///   Timer              → start, or pause a running timer
    ///
    /// The extension opens the tools panel and the tool's own sub-panel on the way, so this
    /// works from anywhere in the call. What happens after the press is Meet's business:
    /// a poll opens a composer, a recording may ask for consent. This gets you to the point
    /// where the only thing left is the part that genuinely needs a human.
    ///
    /// Live streaming has no single button and is not offered here — use Open Meeting Tool.
    /// </summary>
    public class StartMeetingToolAction : PluginAction
    {
        public override string Name => "Start Meeting Tool";

        public override string Description =>
            "Press the main button of a Google Meet tool: start a recording, transcript, poll, " +
            "question, breakout setup or translation";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<MeetingTool>(this, "Tool");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var tool = ChoiceConfig<MeetingTool>.LoadConfig(Configuration)?.Choice ?? MeetingTool.Record;
                MeetHelper.SendMeetingTool(tool, start: true, $"{Name} ({tool})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
