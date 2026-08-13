using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Shuffles everyone into rooms at random, clears those assignments again, or opens
    /// the rooms. <b>Open rooms</b> is the one that actually moves people.
    ///
    /// Assigning specific people to specific rooms is drag-and-drop and stays a mouse job —
    /// but Shuffle does the assignment for you, so <i>Shuffle</i> then <i>Open rooms</i>
    /// runs the whole feature from two keys.
    ///
    /// Opening the editor in the first place is <i>Start Meeting Tool → Breakout rooms</i>;
    /// these three navigate there on their own if it is not already showing.
    /// </summary>
    public class BreakoutRoomsAction : PluginAction
    {
        public override string Name => "Breakout Rooms";

        public override string Description =>
            "Shuffle people into Google Meet breakout rooms, clear them, or open the rooms";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<BreakoutAction>(this, "Breakout action");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var action = ChoiceConfig<BreakoutAction>.LoadConfig(Configuration)?.Choice
                    ?? BreakoutAction.Shuffle;
                MeetHelper.SendBreakoutAction(action, $"{Name} ({action})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
