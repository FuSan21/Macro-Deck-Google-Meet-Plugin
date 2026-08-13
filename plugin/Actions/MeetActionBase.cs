using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;
using System;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Base for the Google Meet actions. Each one sends a single named command to every
    /// connected Meet tab; the browser extension is what actually clicks the button.
    ///
    /// There is no fallback if no tab is connected. Meet is a web page — with the extension
    /// gone there is nothing left to talk to, so the action logs why it did nothing rather
    /// than pretending it worked.
    /// </summary>
    public abstract class MeetActionBase : PluginAction
    {
        public override bool CanConfigure => false;

        /// <summary>The protocol event name sent to the extension.</summary>
        protected abstract string EventName { get; }

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                MeetHelper.Send(EventName, Name);
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Unexpected Exception:\n{0}", ex);
            }
        }
    }
}
