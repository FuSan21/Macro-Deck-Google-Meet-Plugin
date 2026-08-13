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
        this._start(message.minutes, message.seconds);
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
   * The Minutes and Seconds boxes, in that order. Only editable while stopped, and they
   * carry Google's generic input jsname, so order is the only thing distinguishing them.
   *
   * Note that Meet runs its timers about 23 seconds longer than asked — a 10 second timer
   * counts down from 33, a 12:30 one from 12:53. That is Meet's own doing, not this
   * extension's: its untouched 5:00 default starts at 5:23 with nothing written to either
   * box. Deliberately not compensated for, since a hidden -23s here would disagree with
   * the duration Meet itself displays, and would start under-running the moment Google
   * fixes it.
   */
  static DurationInputSelector = '[jsname="YPqjbf"]';

  /**
   * Writes a value into one of Meet's number boxes.
   *
   * Assigning to `.value` alone changes what is on screen and nothing else — Meet is
   * listening for the `input` event, not for the property — so the write goes through the
   * prototype's own setter and the event is raised by hand. Setting the property directly
   * would be silently swallowed by the framework's value tracking.
   */
  static _setInput = (input, value) => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    input.focus();
    setter.call(input, String(value));
    input.dispatchEvent(new Event("input", { bubbles: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
    input.blur();
  }

  /**
   * Starts a stopped timer, or resumes a held one, and does nothing at all if it is
   * already counting down — so a key bound to Start is safe to press twice, and safe to
   * press when somebody else already started the timer.
   *
   * A duration is only applied from a standing start. Meet fixes the length when a timer
   * begins and disables the boxes, so a paused timer is resumed at whatever it has left
   * rather than being restarted at a new length, which is what "resume" ought to mean.
   */
  _start = async (minutes, seconds) => {
    const state = TimerEventHandler.state();
    if (state === "running") {
      return;
    }

    if (state === "paused") {
      // Resuming can go through the tray, which leaves the side panel alone.
      await this._act(
        TimerEventHandler.TrayStartPauseSelector,
        TimerEventHandler.StartPauseSelector,
        "resume the timer");
      return;
    }

    // Starting from nothing needs the panel: there is no chip to hover, and the duration
    // boxes only exist there.
    if (!await this._ensureTimerPanel()) {
      console.error("Could not reach Meet's timer to start it.");
      return;
    }

    if (Number.isInteger(minutes) || Number.isInteger(seconds)) {
      const inputs = [...document.querySelectorAll(TimerEventHandler.DurationInputSelector)]
        .filter((i) => i.getClientRects().length && !i.disabled);

      if (inputs.length < 2) {
        console.error(
          `Expected the timer's Minutes and Seconds boxes but found ${inputs.length}. ` +
          "Starting at whatever duration Meet is showing instead.");
      } else {
        TimerEventHandler._setInput(inputs[0], Number.isInteger(minutes) ? minutes : 0);
        TimerEventHandler._setInput(inputs[1], Number.isInteger(seconds) ? seconds : 0);
        await new Promise((resolve) => setTimeout(resolve, 300));
      }
    }

    document.querySelector(TimerEventHandler.StartPauseSelector)?.click();
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
