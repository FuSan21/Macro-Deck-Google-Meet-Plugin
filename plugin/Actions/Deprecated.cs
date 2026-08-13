using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Stand-ins for actions that no longer exist.
    ///
    /// Macro Deck stores an action's full type name in the profile and rebuilds it with
    /// Json.NET. If the type is gone the profile does not merely lose that button — the
    /// whole file fails to deserialize, Macro Deck loads a blank profile in its place, and
    /// the next save writes the blank over it. Every button in the profile is lost, not
    /// just the orphaned one, and there is no backup to go back to. That happened once,
    /// on 2026-08-13, when Toggle Host Control was replaced by Apply Host Controls.
    ///
    /// So removed actions leave a shell behind instead. It is deliberately not registered
    /// in <see cref="GoogleMeetPlugin.Enable"/>, so it never appears in the action picker
    /// and cannot be added to anything new — it exists only so old profiles still load.
    /// Pressing one says what replaced it rather than failing silently.
    ///
    /// These can be deleted once nobody could still be holding a profile that names them,
    /// which for a plugin that has never been in the extension store means: not yet.
    /// </summary>
    public abstract class RemovedAction : PluginAction
    {
        public override bool CanConfigure => false;

        /// <summary>What to use instead.</summary>
        protected abstract string Replacement { get; }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            PluginInstance.Logger.Warning(
                "\"{0}\" no longer exists — rebind this button to {1}.", Name, Replacement);
        }
    }

    public class ToggleHostControlAction : RemovedAction
    {
        public override string Name => "Toggle Host Control (removed)";
        public override string Description => "Replaced by Apply Host Controls";
        protected override string Replacement => "Apply Host Controls";
    }

    public class SetMeetingAccessAction : RemovedAction
    {
        public override string Name => "Set Meeting Access (removed)";
        public override string Description => "Replaced by Apply Host Controls";
        protected override string Replacement => "Apply Host Controls";
    }

    public class ToggleHostControlsAction : RemovedAction
    {
        public override string Name => "Toggle Host Controls (removed)";
        public override string Description => "Replaced by Apply Host Controls";
        protected override string Replacement => "Apply Host Controls";
    }

    public class ToggleMeetingToolsAction : RemovedAction
    {
        public override string Name => "Toggle Meeting Tools (removed)";
        public override string Description => "Replaced by Open Meeting Tool";
        protected override string Replacement => "Open Meeting Tool";
    }

    public class ToggleMeetingDetailsAction : RemovedAction
    {
        public override string Name => "Toggle Meeting Details (removed)";
        public override string Description => "Removed; open the panel in Meet instead";
        protected override string Replacement => "nothing — open the panel in Meet";
    }

    public class ToggleTranscriptionAction : RemovedAction
    {
        public override string Name => "Toggle Transcription (removed)";
        public override string Description => "Replaced by Start Meeting Tool";
        protected override string Replacement => "Start Meeting Tool, set to Transcribe";
    }

    public class RecordingOptionAction : RemovedAction
    {
        public override string Name => "Recording Option (removed)";
        public override string Description => "Removed; set these in Meet's recording panel";
        protected override string Replacement => "nothing — tick these in Meet's recording panel";
    }
}
