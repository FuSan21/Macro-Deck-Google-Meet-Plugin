using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Everything Meet's breakout rooms offer: shuffle people into them, open and close
    /// them, join one yourself, come back, and set the countdown that ends them.
    ///
    /// The controls live in two places — the room list, and the editor behind
    /// "Set up"/"Edit rooms" — and this navigates to whichever the chosen command needs, so
    /// a key works from anywhere in the call.
    ///
    /// Assigning specific people to specific rooms is drag-and-drop and stays a mouse job,
    /// but Shuffle does the assignment for you, so <i>Shuffle</i> then <i>Open rooms</i>
    /// runs the whole feature from two keys.
    ///
    /// Several commands only exist in one state: Open and Close are opposites, Return to
    /// main call only while you are inside a room, and Shuffle and Clear are greyed out
    /// until somebody else is in the call. Asking for one that is not available is reported
    /// to the browser console rather than pressed.
    /// </summary>
    public class BreakoutRoomsAction : PluginAction
    {
        public override string Name => "Breakout Rooms";

        public override string Description =>
            "Shuffle people into Google Meet breakout rooms, clear them, or open the rooms";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new BreakoutConfig(this);
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var config = BreakoutConfig.LoadConfig(Configuration);
                var action = config?.Action ?? BreakoutAction.Shuffle;
                var room = config?.Room ?? 1;
                var minutes = config?.Minutes ?? 30;

                var summary = action == BreakoutAction.JoinRoom ? $"{action} {room}"
                    : action == BreakoutAction.SetTimer ? $"{action} {minutes}min"
                    : action.ToString();

                MeetHelper.SendBreakoutAction(action, room, minutes, $"{Name} ({summary})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
