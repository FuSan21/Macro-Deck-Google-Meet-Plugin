using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Sends one emoji reaction, picked when the button is configured. The extension opens
    /// Meet's reaction palette first if the chosen emoji is not already on screen.
    /// </summary>
    public class SendReactionAction : PluginAction
    {
        public override string Name => "Send Reaction";

        public override string Description =>
            "Send an emoji reaction in Google Meet: Heart, Thumbs up, Celebrate, Clap, Joy, " +
            "Surprised, Sad, Thinking or Thumbs down";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<ReactionType>(this, "Reaction");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var reaction = ChoiceConfig<ReactionType>.LoadConfig(Configuration)?.Choice ?? ReactionType.Heart;
                MeetHelper.SendReaction(reaction, $"{Name} ({reaction})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
