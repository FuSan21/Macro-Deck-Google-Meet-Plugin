using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Flips one switch in Meet's Host controls panel — who may share, unmute, turn on
    /// video, react, chat, or ask questions.
    ///
    /// Only the host has this panel; for anyone else Meet does not render it at all, and
    /// the extension says so in the browser console rather than pressing something. Most of
    /// the switches are also greyed out until <c>Host management</c> is on, so that is
    /// usually the first button to bind.
    ///
    /// These are the one part of the plugin that is English-only. Every switch shares the
    /// same automation attribute and none has an icon, so the visible label is the only
    /// thing that distinguishes them — and Meet translates it.
    /// </summary>
    public class ToggleHostControlAction : PluginAction
    {
        public override string Name => "Toggle Host Control";

        public override string Description =>
            "Flip one Google Meet host control, such as whether contributors may share their " +
            "screen or unmute. Host only, and English only";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<HostControl>(this, "Host control");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var control = ChoiceConfig<HostControl>.LoadConfig(Configuration)?.Choice
                    ?? HostControl.HostManagement;
                MeetHelper.SendHostControl(control, $"{Name} ({control})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
