using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Ticks or unticks one of the three options Meet offers on the Recording panel before
    /// a recording starts: include captions, also start a transcript, also start Take Notes
    /// with Gemini. The last of those is on by default, which is worth knowing — a recording
    /// started without touching anything also produces a Gemini notes document.
    ///
    /// Only settable before the recording begins. Bind these ahead of a
    /// <see cref="StartMeetingToolAction"/> on Record to get a repeatable setup.
    /// </summary>
    public class RecordingOptionAction : PluginAction
    {
        public override string Name => "Recording Option";

        public override string Description =>
            "Tick or untick a Google Meet recording option: captions, transcript or Gemini notes";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<RecordingOption>(this, "Recording option");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var option = ChoiceConfig<RecordingOption>.LoadConfig(Configuration)?.Choice
                    ?? RecordingOption.IncludeCaptions;
                MeetHelper.SendRecordingOption(option, $"{Name} ({option})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
