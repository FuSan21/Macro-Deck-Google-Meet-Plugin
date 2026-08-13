using System.Collections.Generic;

namespace FuSan21.MacroDeck.GoogleMeet
{
    /// <summary>
    /// Everything the browser extension has told us about the call, plus whether it is
    /// still talking to us at all.
    ///
    /// Unlike the Zoom and Teams plugins this state is pushed, not polled: the extension
    /// sends one event per control whenever that control changes, and re-sends all of them
    /// when a tab connects. So a field is only as fresh as the last event for that field —
    /// which is fine, because Meet's DOM cannot change without the MutationObserver firing.
    ///
    /// The one thing that cannot be inferred from the upstream events is whether you are in
    /// a call: the microphone and camera buttons exist on the join screen too, so their
    /// presence proves nothing. <see cref="InCallKnown"/> records whether we have heard an
    /// explicit answer, which only our forked extension sends.
    /// </summary>
    public class MeetState
    {
        /// <summary>At least one Meet tab has an open socket to us.</summary>
        public bool Connected { get; set; }

        /// <summary>
        /// True while the extension reports an active call. When <see cref="InCallKnown"/>
        /// is false this is a stand-in meaning "a Meet tab is connected", because the
        /// official extension gives us nothing better.
        /// </summary>
        public bool InMeeting { get; set; }

        /// <summary>
        /// Whether <see cref="InMeeting"/> came from a real in-call signal rather than the
        /// connected-tab stand-in. Only the forked extension sends one, so this doubles as
        /// "the user installed our extension".
        /// </summary>
        public bool InCallKnown { get; set; }

        public bool IsMuted { get; set; }
        public bool IsVideoOn { get; set; }
        public bool IsHandRaised { get; set; }
        public bool AreCaptionsOn { get; set; }
        public bool IsPresentationPinned { get; set; }

        /// <summary>Fork only; stays false with the official extension.</summary>
        public bool IsPresenting { get; set; }

        public static MeetState Empty => new MeetState();

        public MeetState Clone() => (MeetState)MemberwiseClone();

        public bool Equals(MeetState other)
        {
            if (other == null) return false;
            return Connected == other.Connected
                && InMeeting == other.InMeeting
                && InCallKnown == other.InCallKnown
                && IsMuted == other.IsMuted
                && IsVideoOn == other.IsVideoOn
                && IsHandRaised == other.IsHandRaised
                && AreCaptionsOn == other.AreCaptionsOn
                && IsPresentationPinned == other.IsPresentationPinned
                && IsPresenting == other.IsPresenting;
        }

        /// <summary>
        /// The flags published as <c>meet_*</c> variables, in the order they appear as
        /// parameter suggestions on the state-changed event.
        /// </summary>
        public IEnumerable<(string Name, bool Value)> TrackedFlags()
        {
            yield return ("connected", Connected);
            yield return ("in_meeting", InMeeting);
            yield return ("is_muted", IsMuted);
            yield return ("is_video_on", IsVideoOn);
            yield return ("is_hand_raised", IsHandRaised);
            yield return ("are_captions_on", AreCaptionsOn);
            yield return ("is_presentation_pinned", IsPresentationPinned);
            yield return ("is_presenting", IsPresenting);
        }
    }
}
