/**
 * Starts and stops presenting, and reports which of the two you are doing.
 *
 * ADDED BY THIS FORK.
 *
 * The control is found by its Material Symbols ligature rather than by a jsname or an
 * aria-label. That is the same trick PinPresentationEventHandler already uses, and it is
 * the only one of the three that is both stable and language-independent:
 *
 *   - jsname values are minifier output. Meet has changed the microphone's twice, which
 *     is why upstream's MicEventHandler carries three selectors.
 *   - aria-labels are real UI copy and are translated, so matching them works in English
 *     and silently fails everywhere else.
 *   - the ligature inside <i class="google-material-icons"> is the icon's *name*. It is
 *     English-looking, but it is an identifier in Google's icon font, not a string shown
 *     to the user, so it does not translate.
 *
 * Which ligature Meet uses has to be confirmed against a live call, so both plausible
 * names for each state are listed. Anything unmatched simply means no button is found,
 * the plugin is told nothing, and `meet_is_presenting` stays false — no wrong clicking.
 */
class PresentEventHandler extends SDEventHandler {

  /** Ligatures shown while you are NOT presenting: pressing this starts a share. */
  static StartIcons = ["present_to_all", "screen_share"];

  /** Ligatures shown while you ARE presenting: pressing this stops the share. */
  static StopIcons = ["cancel_presentation", "stop_screen_share", "present_to_all_off"];

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
    this._lastReported = null;
    this._sendState();
  }

  handleStreamDeckEvent = (message) => {
    if (message.event === "togglePresent") {
      this._togglePresent();
    } else if (message.event === "getPresentingState") {
      this._lastReported = null;
      this._sendState();
    }
  }

  /**
   * Returns `{ button, presenting }`, or null when no presenting control is on the page —
   * which is the normal state before a call starts.
   */
  _findControl = () => {
    const icons = document.querySelectorAll("i.google-material-icons, i.material-icons-extended, i.google-symbols");
    let start = null;

    for (const icon of icons) {
      const ligature = icon.textContent.trim();
      const button = icon.closest('button, [role="button"]');
      if (!button) {
        continue;
      }

      // A stop icon wins outright wherever it appears: it can only exist while a share
      // is running, whereas a start icon may also be sitting in an overflow menu.
      if (PresentEventHandler.StopIcons.includes(ligature)) {
        return { button: button, presenting: true };
      }
      if (!start && PresentEventHandler.StartIcons.includes(ligature)) {
        start = { button: button, presenting: false };
      }
    }

    return start;
  }

  /**
   * Note that starting a share cannot finish here. Chrome will not hand a page a screen
   * without the user picking one in its own picker, so all this can do is open that
   * picker. Stopping needs no confirmation and completes on its own.
   */
  _togglePresent = () => {
    const control = this._findControl();
    if (!control) {
      throw new ControlsNotFoundError("No presenting button found in the Meet UI!");
    }

    control.button.click();
    this._scheduleCheck();
  }

  _scheduleCheck = () => {
    if (this._pending) {
      return;
    }
    this._pending = setTimeout(() => {
      this._pending = null;
      this._sendState();
    }, PresentEventHandler.DebounceMs);
  }

  _sendState = () => {
    const control = this._findControl();
    const presenting = Boolean(control && control.presenting);

    if (presenting === this._lastReported) {
      return;
    }

    this._lastReported = presenting;
    this._connectionManager.sendMessage({
      event: "presentingState",
      presenting: presenting,
    });
  }

}
