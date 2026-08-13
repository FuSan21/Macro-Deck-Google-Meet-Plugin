using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Opens one card in Meet's "Meeting tools" side panel — Record, Transcribe, Polls,
    /// Q&amp;A, Breakout rooms, Speech translation, Timer or Live streaming.
    ///
    /// These are not toolbar buttons: the panel has to be opened and the card pressed, and
    /// most of them then present their own sub-panel where the actual work happens. So this
    /// action gets you *to* the tool in one press; it does not stand in for the tool.
    ///
    /// Which cards a meeting offers depends on the host's Workspace plan and on whether you
    /// are the host. A tool that is not on offer is reported to the browser console, with
    /// the list of cards that were there instead — nothing else is pressed.
    /// </summary>
    public class OpenMeetingToolAction : PluginAction
    {
        public override string Name => "Open Meeting Tool";

        public override string Description =>
            "Open a Google Meet tool: Record, Transcribe, Polls, Q&A, Breakout rooms, " +
            "Speech translation, Timer or Live streaming";

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
                MeetHelper.SendMeetingTool(tool, $"{Name} ({tool})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
