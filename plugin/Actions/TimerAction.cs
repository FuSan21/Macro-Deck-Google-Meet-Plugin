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
    /// Start and Pause are separate even though Meet drives both from one button. Each does
    /// nothing when the timer is already in the state it would produce, so a key labelled
    /// Start never pauses — which matters when you cannot see the panel, or when somebody
    /// else started the timer.
    ///
    /// Once a timer exists these go through the tray hidden behind the top-bar chip, so the
    /// side panel is left alone and whatever you had open — chat, participants, another
    /// tool — stays open. Starting from nothing has to use the panel, because with no timer
    /// there is no chip.
    ///
    /// Setting the duration is still a keyboard job: that is two text fields, not a button.
    /// Add a minute is the one adjustment that has a control of its own.
    /// </summary>
    public class TimerAction : PluginAction
    {
        public override string Name => "Timer";

        public override string Description =>
            "Start, pause, stop or extend the Google Meet countdown timer";

        public override bool CanConfigure => true;

        public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
        {
            return new TimerConfig(this);
        }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                var config = TimerConfig.LoadConfig(Configuration);
                var command = config?.Command ?? TimerCommand.Start;

                if (command == TimerCommand.Start &&
                    TimerConfig.TryParseDuration(config?.Duration, out var minutes, out var seconds))
                {
                    MeetHelper.SendTimerStart(minutes, seconds, $"{Name} (Start {minutes}:{seconds:00})");
                    return;
                }

                MeetHelper.Send(TimerCommands.For(command), $"{Name} ({command})");
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
