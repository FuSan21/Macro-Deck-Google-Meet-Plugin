using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Applies a whole saved host-controls configuration in one press — who may share,
    /// unmute, react, chat, ask questions, and who may join at all.
    ///
    /// One button per *configuration* rather than per switch, laid out the way Meet lays
    /// the panel out, so a "lock it down" key and an "open it up" key are one press each
    /// instead of eight.
    ///
    /// Every setting is set rather than toggled: the extension reads the current state and
    /// only clicks the rows that disagree. So the key is idempotent, and lands on the same
    /// result whether or not somebody has already changed things by hand.
    ///
    /// Only the host has this panel; for anyone else Meet does not render it at all, and
    /// the extension says so in the browser console rather than pressing something. It is
    /// also the one part of the plugin that is English-only, because Meet's rows share the
    /// same automation attributes and carry no icon, leaving the translated label as the
    /// only thing that tells them apart.
    /// </summary>
    public class ApplyHostControlsAction : PluginAction
    {
        public override string Name => "Apply Host Controls";

        public override string Description =>
            "Apply a saved set of Google Meet host controls — sharing, unmuting, chat, Q&A " +
            "and meeting access — in one press. Host only, and English only";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new HostControlsConfig(this);
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var settings = HostControlsConfig.LoadConfig(Configuration);
                if (settings == null ||
                    ((settings.Controls == null || settings.Controls.Count == 0) && !settings.Access.HasValue))
                {
                    PluginInstance.Logger.Warning("{0}: nothing is set to change", Name);
                    return;
                }

                var count = (settings.Controls?.Count ?? 0) + (settings.Access.HasValue ? 1 : 0);
                MeetHelper.SendHostControls(settings.Controls, settings.Access, $"{Name} ({count} settings)");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
