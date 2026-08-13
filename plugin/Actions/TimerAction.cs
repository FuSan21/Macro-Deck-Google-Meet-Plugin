using FuSan21.MacroDeck.GoogleMeet.UI;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Drives Meet's countdown timer without you having to be looking at it. The timer sits
    /// two levels deep — tools panel, then the Timer card's own sub-panel — and the
    /// extension navigates there if it needs to, so the key works from anywhere in the call.
    ///
    /// Setting the duration is still a keyboard job: that is two text fields, not a button.
    /// Add a minute is the one adjustment that has a control of its own.
    /// </summary>
    public class TimerAction : PluginAction
    {
        public override string Name => "Timer";

        public override string Description =>
            "Start, pause, cancel or extend the Google Meet countdown timer";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new ChoiceConfig<TimerCommand>(this, "Timer action");
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var command = ChoiceConfig<TimerCommand>.LoadConfig(Configuration)?.Choice
                    ?? TimerCommand.StartOrPause;
                MeetHelper.Send(TimerCommands.For(command), $"{Name} ({command})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
