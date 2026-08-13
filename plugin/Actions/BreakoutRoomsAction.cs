using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Drives Meet's Breakout rooms editor: open the editor, shuffle everyone at random,
    /// clear the assignments, set the rooms' countdown, discard changes, or open the rooms.
    ///
    /// <b>Open rooms</b> is the one that actually moves people. The rest set it up.
    ///
    /// Assigning specific people to specific rooms is drag-and-drop, so it stays a mouse
    /// job — but Shuffle does the assignment for you, which makes "shuffle, then open" a
    /// two-key sequence that runs the whole thing.
    /// </summary>
    public class BreakoutRoomsAction : PluginAction
    {
        public override string Name => "Breakout Rooms";

        public override string Description =>
            "Set up, shuffle, clear or open Google Meet breakout rooms";

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
                    ?? BreakoutAction.SetUp;
                MeetHelper.SendBreakoutAction(action, $"{Name} ({action})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
