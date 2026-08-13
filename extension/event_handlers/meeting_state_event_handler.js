/**
 * Reports whether you are actually in a call.
 *
 * ADDED BY THIS FORK. The upstream extension has no such event, and none of its state
 * events can stand in for one: the microphone and camera buttons exist on the green room
 * / "Ask to join" screen exactly as they do in a call, so their presence proves only that
 * a Meet page is loaded. The one control that appears when the call starts and disappears
 * the moment it ends is the leave button, so that is the signal.
 *
 * Unlike the mute-shaped events this one is not a toggle, so it does not extend
 * ToggleEventHandler and its payload is `inCall` rather than `muted`.
 */
class MeetingStateEventHandler extends SDEventHandler {

  /**
   * The same selector upstream's LeaveCallEventHandler clicks, which is what keeps the
   * two in step: if this ever stops matching, Leave Call breaks at the same moment and
   * both are fixed by the same one-line change.
   */
  static LeaveButtonSelector = '[jsname="CQylAd"]';

  /**
   * Meet mutates its DOM constantly — every video tile, every timer tick. Coalescing
   * means one querySelector per quiet moment instead of one per batch of mutations.
   */
  static DebounceMs = 250;

  _lastReported = null;
  _pending = null;

  initialize = () => {
    const observer = new MutationObserver(this._scheduleCheck);
    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
    this._sendState();
  }

  onNewStreamDeckConnection = () => {
    // A reconnect means the desktop side has forgotten everything, so re-send even if
    // nothing has changed since last time.
    this._lastReported = null;
    this._sendState();
  }

  handleStreamDeckEvent = (message) => {
    if (message.event === "getMeetingState") {
      this._lastReported = null;
      this._sendState();
    }
  }

  _isInCall = () => {
    return Boolean(document.querySelector(MeetingStateEventHandler.LeaveButtonSelector));
  }

  _scheduleCheck = () => {
    if (this._pending) {
      return;
    }
    this._pending = setTimeout(() => {
      this._pending = null;
      this._sendState();
    }, MeetingStateEventHandler.DebounceMs);
  }

  _sendState = () => {
    const inCall = this._isInCall();
    if (inCall === this._lastReported) {
      return;
    }

    this._lastReported = inCall;
    this._connectionManager.sendMessage({
      event: "meetingState",
      inCall: inCall,
    });
  }

}
