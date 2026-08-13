using Newtonsoft.Json.Linq;
using System;

namespace FuSan21.MacroDeck.GoogleMeet
{
    /// <summary>
    /// The single point the actions and the UI talk to. Owns the <see cref="MeetServer"/>,
    /// folds the events it receives into a <see cref="MeetState"/>, and hands finished
    /// states to the plugin so they become Macro Deck variables.
    /// </summary>
    internal static class MeetHelper
    {
        private static readonly MeetServer _server = new MeetServer();
        private static readonly object _stateLock = new object();

        private static MeetState _state = MeetState.Empty;
        private static bool _wired;

        /// <summary>Raised when a tab connects or disconnects, so the status icon can follow.</summary>
        public static event EventHandler AvailabilityChanged;

        /// <summary>True when at least one Meet tab is connected — i.e. commands will go somewhere.</summary>
        public static bool IsAvailable => _server.HasClients;

        public static bool IsRunning => _server.IsRunning;

        public static int ConnectedTabs => _server.ClientCount;

        public static string LastError => _server.LastError;

        public static MeetState State
        {
            get { lock (_stateLock) { return _state.Clone(); } }
        }

        public static void Start(Configuration configuration)
        {
            if (!_wired)
            {
                _server.MessageReceived += Server_MessageReceived;
                _server.ClientsChanged += Server_ClientsChanged;
                _wired = true;
            }

            _server.Start(configuration?.Port ?? Configuration.DefaultPort);
            AvailabilityChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Stop()
        {
            _server.Stop();
            Reset();
        }

        /// <summary>Drops every remembered flag. Used when the server stops or the last tab leaves.</summary>
        private static void Reset()
        {
            MeetState previous;
            lock (_stateLock)
            {
                previous = _state;
                _state = MeetState.Empty;
            }

            PluginInstance.Plugin?.ApplyState(previous, MeetState.Empty);
        }

        #region Sending

        /// <summary>
        /// Sends a bare <c>{ "event": name }</c> command to every connected tab.
        /// <paramref name="description"/> only ever appears in the log.
        /// </summary>
        public static bool Send(string eventName, string description)
        {
            return Send(new JObject { ["event"] = eventName }, description);
        }

        public static bool SendReaction(ReactionType reaction, string description)
        {
            var message = new JObject
            {
                ["event"] = MeetProtocol.Outbound.EmojiReact,
                ["emojiChar"] = ReactionEmoji.For(reaction),
            };

            return Send(message, description);
        }

        /// <summary>
        /// Opens one card in Meet's "Meeting tools" side panel, or presses that tool's main
        /// button when <paramref name="start"/> is set.
        /// </summary>
        public static bool SendMeetingTool(MeetingTool tool, bool start, string description)
        {
            var message = new JObject
            {
                ["event"] = start
                    ? MeetProtocol.Outbound.StartMeetingTool
                    : MeetProtocol.Outbound.OpenMeetingTool,
                ["tool"] = MeetingToolNames.For(tool),
            };

            return Send(message, description);
        }

        /// <summary>Presses one button in Meet's Breakout rooms editor.</summary>
        public static bool SendBreakoutAction(BreakoutAction action, string description)
        {
            var message = new JObject
            {
                ["event"] = MeetProtocol.Outbound.BreakoutAction,
                ["action"] = BreakoutActionNames.For(action),
            };

            return Send(message, description);
        }

        /// <summary>Flips one switch in Meet's Host controls panel.</summary>
        public static bool SendHostControl(HostControl control, string description)
        {
            var message = new JObject
            {
                ["event"] = MeetProtocol.Outbound.ToggleHostControl,
                ["control"] = HostControlNames.For(control),
            };

            return Send(message, description);
        }

        private static bool Send(JObject message, string description)
        {
            if (!_server.IsRunning)
            {
                PluginInstance.Logger.Warning("{0}: the Google Meet integration is not running", description);
                return false;
            }

            if (!_server.Broadcast(message))
            {
                PluginInstance.Logger.Warning(
                    "{0}: no Google Meet tab is connected — is the browser extension installed and a Meet tab open?",
                    description);
                return false;
            }

            PluginInstance.Logger.Trace("{0}: sent {1}", description, message.ToString(Newtonsoft.Json.Formatting.None));
            return true;
        }

        #endregion

        #region Receiving

        private static void Server_ClientsChanged(object sender, EventArgs e)
        {
            if (!_server.HasClients)
            {
                // Nothing is reporting any more, so every flag we hold is a guess. Clearing
                // is the honest answer; the extension re-sends everything on reconnect.
                Reset();
            }
            else
            {
                Apply(state => state.Connected = true);
            }

            AvailabilityChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void Server_MessageReceived(object sender, JObject message)
        {
            var name = message.Value<string>("event");
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            // Upstream's toggles all report themselves as "muted", where muted means the
            // inactive side of the control. Camera, hand, captions and pinning read more
            // naturally the other way round, so they are inverted here rather than in
            // every action and every button the user configures.
            switch (name)
            {
                case MeetProtocol.Inbound.MicState:
                    Apply(state => state.IsMuted = Muted(message));
                    break;

                case MeetProtocol.Inbound.CameraState:
                    Apply(state => state.IsVideoOn = !Muted(message));
                    break;

                case MeetProtocol.Inbound.HandState:
                    Apply(state => state.IsHandRaised = !Muted(message));
                    break;

                case MeetProtocol.Inbound.CaptionsState:
                    Apply(state => state.AreCaptionsOn = !Muted(message));
                    break;

                case MeetProtocol.Inbound.PinPresentationState:
                    Apply(state => state.IsPresentationPinned = !Muted(message));
                    break;

                case MeetProtocol.Inbound.MeetingState:
                    Apply(state =>
                    {
                        state.InCallKnown = true;
                        state.InMeeting = message.Value<bool?>("inCall") ?? false;
                    });
                    break;

                case MeetProtocol.Inbound.PresentingState:
                    Apply(state => state.IsPresenting = message.Value<bool?>("presenting") ?? false);
                    break;

                default:
                    PluginInstance.Logger.Trace("Ignoring unknown event from Google Meet: {0}", name);
                    break;
            }
        }

        private static bool Muted(JObject message) => message.Value<bool?>("muted") ?? false;

        /// <summary>
        /// Applies one change and publishes the result if anything actually moved. The
        /// connected flag and the in-meeting fallback are recomputed every time, so they
        /// can never drift away from what the server knows.
        /// </summary>
        private static void Apply(Action<MeetState> change)
        {
            MeetState previous, current;

            lock (_stateLock)
            {
                previous = _state;
                current = _state.Clone();
                change(current);

                current.Connected = _server.HasClients;
                if (!current.InCallKnown)
                {
                    // The official extension never says whether you are actually in a call,
                    // so the best available answer is "a Meet tab is talking to us".
                    current.InMeeting = current.Connected;
                }

                if (current.Equals(previous))
                {
                    return;
                }

                _state = current;
            }

            PluginInstance.Plugin?.ApplyState(previous, current);
        }

        #endregion
    }
}
