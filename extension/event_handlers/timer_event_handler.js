/**
 * Meet's countdown timer.
 *
 * ADDED BY THIS FORK.
 *
 * There are two ways to reach the controls, and this prefers the quieter one.
 *
 * While a timer exists Meet puts a chip in the top bar, and hovering that chip opens a
 * little tray holding Pause/resume, Stop and Add 1 minute. That tray is the good route: it
 * needs no side panel, so pressing a deck key does not throw away whatever the user had
 * open — chat, participants, another tool. The tray's buttons are built when it opens and
 * removed when it closes, so it has to be opened first; a synthetic `pointerenter` does
 * that, which is how a content script can use a hover-only control.
 *
 * The side panel is the fallback, and the only route that can *start* a timer, since with
 * no timer running there is no chip to hover.
 *
 * Selectors read off a live call in August 2026.
 */
class TimerEventHandler extends SDEventHandler {

  // --- The hover tray, reached from the top-bar chip ---

  static TrayStartPauseSelector = '[jsname="JmplG"]';

  static TrayStopSelector = '[jsname="Y916Ve"]';

  static TrayAddMinuteSelector = '[jsname="ETuNZc"]';

  // --- The side panel, two levels into Meeting tools ---

  /**
   * Meet uses one button for start, pause and resume. Its label changes but its
   * `aria-label` stays "Pause/resume timer" in all three states, so the button itself
   * cannot tell you which one you are about to do — see {@link TimerEventHandler.state}.
   */
  static StartPauseSelector = '[jsname="SPCnpb"]';

  static CancelSelector = '[jsname="Fq2ped"]';

  static AddMinuteSelector = '[jsname="xLroh"]';

  /** Whether the timer makes a noise when it runs out. Tray has no equivalent. */
  static AlarmSelector = '[jsname="EAB7Kc"]';

  handleStreamDeckEvent = (message) => {
    switch (message.event) {
      case "timerStart":
        this._start();
        break;
      case "timerPause":
        this._pause();
        break;
      case "timerCancel":
        this._act(TimerEventHandler.TrayStopSelector, TimerEventHandler.CancelSelector, "stop the timer");
        break;
      case "timerAddMinute":
        this._act(TimerEventHandler.TrayAddMinuteSelector, TimerEventHandler.AddMinuteSelector, "add a minute");
        break;
      case "timerToggleAlarm":
        this._act(null, TimerEventHandler.AlarmSelector, "toggle the alarm");
        break;
    }
  }

  /**
   * Which of "idle", "running" and "paused" the timer is in, read off the top-bar chip.
   *
   * The chip is used rather than the button because it is the only signal that is both
   * available without opening anything and independent of the display language: the
   * button's own text ("Start" / "Pause" / "Resume") is translated, whereas the chip's
   * icon is a Material Symbols ligature — `timer` while counting down, `timer_pause`
   * while held — and those are identifiers in Google's icon font, not UI copy.
   *
   * Scoped to the top-bar chips deliberately. The `timer` ligature also appears on the
   * Timer card in the tools panel, which says nothing about whether one is running.
   */
  static state = () => {
    for (const icon of document.querySelectorAll('[jsname="ocqpFe"] i')) {
      const ligature = icon.textContent.trim();
      if (ligature === "timer") {
        return "running";
      }
      if (ligature === "timer_pause") {
        return "paused";
      }
    }
    return "idle";
  }

  static _chip = () =>
    [...document.querySelectorAll('[jsname="ocqpFe"]')]
      .find((e) => e.querySelector("i") &&
        ["timer", "timer_pause"].includes(e.querySelector("i").textContent.trim()) &&
        e.getClientRects().length);

  /**
   * Opens the chip's tray by pretending to hover it. Meet binds `pointerenter` through
   * jsaction, and a dispatched event carries far enough for that — the tray is built and
   * its buttons become clickable, exactly as with a real mouse.
   */
  static _openTray = async () => {
    const chip = TimerEventHandler._chip();
    if (!chip) {
      return false;
    }

    for (const type of ["pointerover", "pointerenter", "mouseover", "mouseenter"]) {
      chip.dispatchEvent(new MouseEvent(type, { bubbles: true, cancelable: true, view: window }));
    }

    return await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(TimerEventHandler.TrayStartPauseSelector)?.getClientRects().length,
      1500);
  }

  /**
   * Navigates to the timer's side panel. Only needed when there is no timer to hover, or
   * for the alarm, which the tray does not carry.
   */
  _ensureTimerPanel = async () => {
    if (document.querySelector(TimerEventHandler.StartPauseSelector)?.getClientRects().length) {
      return true;
    }

    // The chip is a one-click shortcut straight into the timer's own sub-panel, which
    // skips opening the tools panel and hunting for the card.
    const chip = TimerEventHandler._chip();
    if (chip) {
      chip.click();
      if (await MeetingToolsEventHandler.waitFor(
        () => document.querySelector(TimerEventHandler.StartPauseSelector)?.getClientRects().length, 2000)) {
        return true;
      }
    }

    const tools = new MeetingToolsEventHandler(this._connectionManager);
    if (!await tools._ensureToolsPanel()) {
      return false;
    }

    const card = tools._findCard(MeetingToolsEventHandler.Tools.timer);
    if (!card) {
      return false;
    }

    card.querySelector('[role="button"]')?.click();
    return await MeetingToolsEventHandler.waitFor(
      () => document.querySelector(TimerEventHandler.StartPauseSelector)?.getClientRects().length);
  }

  /**
   * Presses a control, through the tray when it offers one and the panel otherwise.
   * `traySelector` may be null for controls the tray does not carry.
   */
  _act = async (traySelector, panelSelector, description) => {
    if (traySelector && await TimerEventHandler._openTray()) {
      const trayButton = document.querySelector(traySelector);
      if (trayButton) {
        trayButton.click();
        return true;
      }
    }

    if (!await this._ensureTimerPanel()) {
      console.error(`Could not reach Meet's timer to ${description}.`);
      return false;
    }

    const button = document.querySelector(panelSelector);
    if (!button) {
      console.error(`No timer button found to ${description} (selector ${panelSelector}).`);
      return false;
    }

    button.click();
    return true;
  }

  /**
   * Starts a stopped timer, or resumes a held one, and does nothing at all if it is
   * already counting down — so a key bound to Start is safe to press twice, and safe to
   * press when somebody else already started the timer.
   */
  _start = async () => {
    const state = TimerEventHandler.state();
    if (state === "running") {
      return;
    }

    // Resuming can go through the tray; starting from nothing cannot, because with no
    // timer there is no chip to hover.
    await this._act(
      state === "paused" ? TimerEventHandler.TrayStartPauseSelector : null,
      TimerEventHandler.StartPauseSelector,
      "start the timer");
  }

  /** Pauses a running timer, and does nothing if it is stopped or already held. */
  _pause = async () => {
    if (TimerEventHandler.state() !== "running") {
      return;
    }

    await this._act(
      TimerEventHandler.TrayStartPauseSelector,
      TimerEventHandler.StartPauseSelector,
      "pause the timer");
  }

}
