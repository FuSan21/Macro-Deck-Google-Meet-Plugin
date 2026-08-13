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
