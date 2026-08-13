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

  /**
   * Ligatures shown while you are NOT presenting: pressing this starts a share.
   *
   * `computer_arrow_up` is the one Meet actually renders as of August 2026, on
   * `button[jsname="hNGZQc"]` (aria-label "Share screen"). The others are earlier names
   * kept as a cushion — an unused entry costs one string comparison.
   */
  static StartIcons = ["computer_arrow_up", "present_to_all", "screen_share"];

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
    const icons = document.querySelectorAll(PresentEventHandler.IconSelector);
    let start = null;

    for (const icon of icons) {
      const ligature = icon.textContent.trim();
      const button = icon.closest('button, [role="button"]');

      // Meet keeps closed menus in the DOM, so an icon existing proves nothing about
      // whether its button is on screen. Skipping the ones with no layout box is what
      // stops a stop-sharing entry parked in a collapsed overflow menu from being read
      // as "already presenting" — which would swallow every attempt to start a share.
      if (!button || !PresentEventHandler.IsVisible(button)) {
        continue;
      }

      // Among visible controls a stop icon wins outright: it can only be rendered while
      // a share is actually running.
      if (PresentEventHandler.StopIcons.includes(ligature)) {
        return { button: button, presenting: true };
      }
      if (!start && PresentEventHandler.StartIcons.includes(ligature)) {
        start = { button: button, presenting: false };
      }
    }

    return start;
  }

  static IconSelector = "i.google-material-icons, i.material-icons-extended, i.google-symbols";

  static IsVisible = (element) => {
    if (element.getClientRects().length === 0) {
      return false;
    }
    return getComputedStyle(element).visibility !== "hidden";
  }

  /**
   * Dumps every ligature currently on the page and says which one, if any, this handler
   * would act on. Meant to be called by hand from the console when presenting misbehaves:
   *
   *   PresentEventHandler.diagnose()
   */
  static diagnose = () => {
    const rows = [];
    for (const icon of document.querySelectorAll(PresentEventHandler.IconSelector)) {
      const button = icon.closest('button, [role="button"]');
      rows.push({
        ligature: icon.textContent.trim(),
        visible: button ? PresentEventHandler.IsVisible(button) : false,
        jsname: button?.getAttribute("jsname") ?? "",
        label: button?.getAttribute("aria-label") ?? "",
      });
    }
    console.table(rows);
    return rows;
  }

  /**
   * Note that starting a share cannot finish here. Chrome will not hand a page a screen
   * without the user picking one in its own picker, so all this can do is open that
   * picker. Stopping needs no confirmation and completes on its own.
   */
  _togglePresent = () => {
    const control = this._findControl();
    if (!control) {
      // Say which ligatures were actually on the page, so the miss can be diagnosed from
      // the console without adding logging first.
      console.error(
        "No presenting button found in the Meet UI. Run PresentEventHandler.diagnose() " +
        "to see every icon on the page, and compare against StartIcons/StopIcons."
      );
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
