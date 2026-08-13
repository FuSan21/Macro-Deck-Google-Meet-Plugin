namespace FuSan21.MacroDeck.GoogleMeet
{
    /// <summary>
    /// The wire protocol spoken with the browser extension.
    ///
    /// These names come from ChrisRegado's streamdeck-googlemeet project and are kept
    /// byte-for-byte compatible on purpose: his published Chrome extension can drive this
    /// plugin without any modification. The two members marked "fork only" are our own
    /// additions and are simply never seen when the official extension is installed.
    ///
    /// A note on the word "muted": upstream models every toggle as a mute, so
    /// <c>muted = true</c> means the microphone is silenced, the camera is off, the hand
    /// is down, captions are off and the presentation is unpinned. The plugin flips the
    /// ones whose natural reading is the other way round before publishing variables.
    /// </summary>
    internal static class MeetProtocol
    {
        /// <summary>Events the extension pushes at us.</summary>
        internal static class Inbound
        {
            public const string MicState = "micMutedState";
            public const string CameraState = "cameraMutedState";
            public const string HandState = "handMutedState";
            public const string CaptionsState = "captionsMutedState";
            public const string PinPresentationState = "pinPresentationMutedState";

            /// <summary>Fork only. <c>{ event, inCall }</c> — not a mute-shaped event.</summary>
            public const string MeetingState = "meetingState";

            /// <summary>Fork only.</summary>
            public const string PresentingState = "presentingState";
        }

        /// <summary>Commands we send to the extension.</summary>
        internal static class Outbound
        {
            public const string ToggleMic = "toggleMic";
            public const string MuteMic = "muteMic";
            public const string UnmuteMic = "unmuteMic";
            public const string GetMicState = "getMicState";

            public const string ToggleCamera = "toggleCamera";
            public const string EnableCamera = "enableCamera";
            public const string DisableCamera = "disableCamera";
            public const string GetCameraState = "getCameraState";

            public const string ToggleHand = "toggleHand";
            public const string GetHandState = "getHandState";

            public const string ToggleCaptions = "toggleCaptions";
            public const string GetCaptionsState = "getCaptionsState";

            public const string TogglePinPresentation = "togglePinPresentation";
            public const string GetPinPresentationState = "getPinPresentationState";

            public const string ToggleChat = "toggleChat";
            public const string ToggleParticipants = "toggleParticipants";
            public const string ToggleZenMode = "toggleZenMode";
            public const string LeaveCall = "leaveCall";
            public const string EmojiReact = "emojiReact";

            /// <summary>Fork only.</summary>
            public const string TogglePresent = "togglePresent";

            /// <summary>Fork only.</summary>
            public const string GetPresentingState = "getPresentingState";

            /// <summary>Fork only.</summary>
            public const string GetMeetingState = "getMeetingState";

            /// <summary>Fork only. Carries a <c>tool</c> field; see <see cref="MeetingToolNames"/>.</summary>
            public const string OpenMeetingTool = "openMeetingTool";

            /// <summary>Fork only. Presses a tool's main button. Carries a <c>tool</c> field.</summary>
            public const string StartMeetingTool = "startMeetingTool";

            /// <summary>Fork only. Carries a <c>control</c> field; see <see cref="HostControlNames"/>.</summary>
            public const string ToggleHostControl = "toggleHostControl";

            /// <summary>Fork only.</summary>
            public const string TimerStart = "timerStart";

            /// <summary>Fork only.</summary>
            public const string TimerPause = "timerPause";

            /// <summary>Fork only.</summary>
            public const string TimerCancel = "timerCancel";

            /// <summary>Fork only.</summary>
            public const string TimerAddMinute = "timerAddMinute";

            /// <summary>Fork only.</summary>
            public const string TimerToggleAlarm = "timerToggleAlarm";

            /// <summary>Fork only. Carries an <c>action</c> field; see <see cref="BreakoutActionNames"/>.</summary>
            public const string BreakoutAction = "breakoutAction";
        }
    }

    /// <summary>
    /// The breakout-room buttons worth a key. Opening the editor is left to
    /// <see cref="MeetingTool.BreakoutRooms"/>, and the form's own Cancel and timer
    /// controls are mouse work — you are looking at the form when you use them.
    /// </summary>
    public enum BreakoutAction
    {
        Shuffle,
        OpenRooms,
        Clear,
    }

    internal static class BreakoutActionNames
    {
        public static string For(BreakoutAction action)
        {
            switch (action)
            {
                case BreakoutAction.OpenRooms: return "openRooms";
                case BreakoutAction.Clear: return "clear";
                default: return "shuffle";
            }
        }
    }

    /// <summary>
    /// The switches in Meet's Host controls panel that are worth reaching for mid-call.
    ///
    /// Meet offers fourteen; the other seven — Ask Gemini, Q&amp;A in live stream, add-on
    /// activities, third-party capture, continuous chat, hide-until-approved, anonymous
    /// questions — are all set once before a webinar starts, and listing them would only
    /// make the seven you might actually hit under pressure harder to find.
    ///
    /// Most stay greyed out until <see cref="HostManagement"/> is on; asking for one of
    /// those first is reported to the browser console rather than pressed.
    /// </summary>
    public enum HostControl
    {
        HostManagement,
        LetContributorsShareScreen,
        LetContributorsUnmute,
        LetContributorsTurnOnVideo,
        LetContributorsSendReactions,
        LetParticipantsSendMessages,
        AllowQuestions,
    }

    internal static class HostControlNames
    {
        /// <summary>Maps to the keys of <c>HostControlsEventHandler.Controls</c> in the extension.</summary>
        public static string For(HostControl control)
        {
            switch (control)
            {
                case HostControl.LetContributorsShareScreen: return "shareScreen";
                case HostControl.LetContributorsSendReactions: return "sendReactions";
                case HostControl.LetContributorsUnmute: return "turnOnMicrophone";
                case HostControl.LetContributorsTurnOnVideo: return "turnOnVideo";
                case HostControl.LetParticipantsSendMessages: return "sendMessages";
                case HostControl.AllowQuestions: return "allowQuestions";
                default: return "hostManagement";
            }
        }
    }

    /// <summary>
    /// The cards in Meet's "Meeting tools" side panel. Which of them a given meeting
    /// offers depends on the host's Workspace plan and on whether you are the host, so
    /// the extension reports a miss to the browser console rather than the plugin
    /// pretending a tool exists everywhere.
    /// </summary>
    public enum MeetingTool
    {
        Record,
        Transcribe,
        Polls,
        QuestionsAndAnswers,
        BreakoutRooms,
        SpeechTranslation,
        Timer,
        LiveStreaming,
    }

    internal static class MeetingToolNames
    {
        /// <summary>Maps to the keys of <c>MeetingToolsEventHandler.Tools</c> in the extension.</summary>
        public static string For(MeetingTool tool)
        {
            switch (tool)
            {
                case MeetingTool.Transcribe: return "transcribe";
                case MeetingTool.Polls: return "polls";
                case MeetingTool.QuestionsAndAnswers: return "questions";
                case MeetingTool.BreakoutRooms: return "breakoutRooms";
                case MeetingTool.SpeechTranslation: return "speechTranslation";
                case MeetingTool.Timer: return "timer";
                case MeetingTool.LiveStreaming: return "liveStreaming";
                default: return "record";
            }
        }
    }

    /// <summary>
    /// What a Timer button press should do.
    ///
    /// Meet drives start, pause and resume from a single button, but they are separate
    /// here: <see cref="Start"/> does nothing when the timer is already counting down and
    /// <see cref="Pause"/> does nothing when it is not, so each key means one thing and
    /// can be pressed twice, or pressed when somebody else already started the timer,
    /// without flipping it to the opposite of what the key says.
    /// </summary>
    public enum TimerCommand
    {
        Start,
        Pause,
        Stop,
        AddOneMinute,
        ToggleAlarm,
    }

    /// <summary>
    /// What Start should do about the timer's alarm. Setting it to a state rather than
    /// toggling means a button labelled "start silently" means it every time, whatever
    /// the last person to touch the timer left behind.
    /// </summary>
    public enum TimerAlarm
    {
        LeaveAsIs,
        On,
        Off,
    }

    internal static class TimerCommands
    {
        public static string For(TimerCommand command)
        {
            switch (command)
            {
                case TimerCommand.Pause: return MeetProtocol.Outbound.TimerPause;
                case TimerCommand.Stop: return MeetProtocol.Outbound.TimerCancel;
                case TimerCommand.AddOneMinute: return MeetProtocol.Outbound.TimerAddMinute;
                case TimerCommand.ToggleAlarm: return MeetProtocol.Outbound.TimerToggleAlarm;
                default: return MeetProtocol.Outbound.TimerStart;
            }
        }
    }

    /// <summary>
    /// The nine reactions Meet's palette offers. The enum name is what the user picks from
    /// the dropdown; <see cref="ReactionEmoji.For"/> turns it into the character the
    /// extension matches against <c>data-emoji</c>.
    /// </summary>
    public enum ReactionType
    {
        Heart,
        ThumbsUp,
        Celebrate,
        Clap,
        Joy,
        Surprised,
        Sad,
        Thinking,
        ThumbsDown,
    }

    internal static class ReactionEmoji
    {
        public static string For(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.ThumbsUp: return "\U0001F44D";
                case ReactionType.Celebrate: return "\U0001F389";
                case ReactionType.Clap: return "\U0001F44F";
                case ReactionType.Joy: return "\U0001F602";
                case ReactionType.Surprised: return "\U0001F62E";
                case ReactionType.Sad: return "\U0001F622";
                case ReactionType.Thinking: return "\U0001F914";
                case ReactionType.ThumbsDown: return "\U0001F44E";
                default: return "\U0001F496";
            }
        }
    }
}
