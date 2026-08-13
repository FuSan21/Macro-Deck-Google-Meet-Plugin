using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Diagnostics;

namespace FuSan21.MacroDeck.GoogleMeet.Actions
{
    /// <summary>
    /// Opens Meet's landing page in the default browser. The only action here that does not
    /// go through the extension — there is no call to talk to yet, which is rather the
    /// point, so it just asks the shell to open a URL.
    /// </summary>
    public class OpenMeetAction : PluginAction
    {
        private const string LandingPage = "https://meet.google.com/landing";

        public override string Name => "Open Google Meet";

        public override string Description => "Open the Google Meet landing page in your default browser";

        public override bool CanConfigure => false;

        public override void Trigger(string clientId, ActionButton actionButton)
        {
            try
            {
                Process.Start(new ProcessStartInfo(LandingPage) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                PluginInstance.Logger.Error("Could not open Google Meet:\n{0}", ex);
            }
        }
    }
}
